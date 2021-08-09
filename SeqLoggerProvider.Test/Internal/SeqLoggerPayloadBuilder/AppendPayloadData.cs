using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayloadBuilder
{
    [TestFixture]
    public class AppendPayloadData
    {
        public static IReadOnlyList<TestCaseData> ChannelHasEvents_TestCaseData()
            => new[]
            {
                /*                  priorityDeliveryLevel,  isReadyToDeliver,   logEventLevels                                                                                                      */
                new TestCaseData(   LogLevel.Trace,         true,               new[] { LogLevel.Trace }                                                                                            ).SetName("{m}(Trace log, Priority)"),
                new TestCaseData(   LogLevel.Debug,         false,              new[] { LogLevel.Trace }                                                                                            ).SetName("{m}(Trace log, Non-Priority)"),
                new TestCaseData(   LogLevel.Debug,         true,               new[] { LogLevel.Debug }                                                                                            ).SetName("{m}(Debug log, Priority)"),
                new TestCaseData(   LogLevel.Information,   false,              new[] { LogLevel.Debug }                                                                                            ).SetName("{m}(Debug log, Non-Priority)"),
                new TestCaseData(   LogLevel.Information,   true,               new[] { LogLevel.Information }                                                                                      ).SetName("{m}(Information log, Priority)"),
                new TestCaseData(   LogLevel.Warning,       false,              new[] { LogLevel.Information }                                                                                      ).SetName("{m}(Information log, Non-Priority)"),
                new TestCaseData(   LogLevel.Warning,       true,               new[] { LogLevel.Warning }                                                                                          ).SetName("{m}(Warning log, Priority)"),
                new TestCaseData(   LogLevel.Error,         false,              new[] { LogLevel.Warning }                                                                                          ).SetName("{m}(Warning log, Non-Priority)"),
                new TestCaseData(   LogLevel.Error,         true,               new[] { LogLevel.Error }                                                                                            ).SetName("{m}(Error log, Priority)"),
                new TestCaseData(   LogLevel.Critical,      false,              new[] { LogLevel.Error }                                                                                            ).SetName("{m}(Error log, Non-Priority)"),
                new TestCaseData(   LogLevel.Critical,      true,               new[] { LogLevel.Critical }                                                                                         ).SetName("{m}(Critical log, Priority)"),
                new TestCaseData(   LogLevel.None,          false,              new[] { LogLevel.Critical }                                                                                         ).SetName("{m}(Critical log, Non-Priority)"),
                new TestCaseData(   LogLevel.Trace,         true,               new[] { LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical } ).SetName("{m}(All levels, ordered)"),
                new TestCaseData(   LogLevel.Critical,      true,               new[] { LogLevel.Critical, LogLevel.Error, LogLevel.Warning, LogLevel.Information, LogLevel.Debug, LogLevel.Trace } ).SetName("{m}(All levels, reverse-ordered)"),
                new TestCaseData(   LogLevel.Trace,         true,               new[] { LogLevel.Information, LogLevel.Trace, LogLevel.Trace }                                                      ).SetName("{m}(Duplicate levels 1)"),
                new TestCaseData(   LogLevel.Information,   true,               new[] { LogLevel.Trace, LogLevel.Information, LogLevel.Trace }                                                      ).SetName("{m}(Duplicate levels 2)"),
                new TestCaseData(   LogLevel.Warning,       false,              new[] { LogLevel.Trace, LogLevel.Trace, LogLevel.Information }                                                      ).SetName("{m}(Duplicate levels 3)")
            };

        [TestCaseSource(nameof(ChannelHasEvents_TestCaseData))]
        public void ChannelHasEvents_AddsEventsToPayload(
            LogLevel                priorityDeliveryLevel,
            bool                    isReadyToDeliver,
            IReadOnlyList<LogLevel> eventLogLevels)
        {
            using var testContext = new TestContext();

            testContext.Configuration.Value = new()
            {
                PriorityDeliveryLevel = priorityDeliveryLevel
            };

            foreach (var i in Enumerable.Range(1, eventLogLevels.Count))
                testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(
                    logLevel:   eventLogLevels[i - 1],
                    state:      i,
                    message:    $"This is test event #{i}"));;

            var eventsSerialized = new List<string>();
            testContext.JsonSerializerOptions.Values.First().Converters.Clear();
            testContext.JsonSerializerOptions.Values.First().Converters.Add(new FakeJsonConverter<SeqLoggerEvent>(
            (writer, value, options) =>
            {
                var serializedEvent = value.BuildMessage();
                writer.WriteStringValue(serializedEvent);
                eventsSerialized.Add(serializedEvent);
            }));

            var uut = testContext.BuildUut();

            using var payloadBuffer = new MemoryStream();
            var payloadEncoding = new UTF8Encoding(false);

            var result = uut.AppendPayloadData(payloadBuffer);

            eventsSerialized.Count.ShouldBe(eventLogLevels.Count);

            payloadEncoding.GetString(payloadBuffer.GetBuffer(), 0, (int)payloadBuffer.Length).ShouldBe(string.Join("", eventsSerialized.Select(x => $"\"{x}\"\n")));
            payloadBuffer.Position.ShouldBe(payloadBuffer.Length);

            result.EventsAdded.ShouldBe((uint)eventLogLevels.Count);
            result.IsDeliveryNeeded.ShouldBe(isReadyToDeliver);
        }

        [Test]
        public void PayloadOverflows_CachesEventUntilPayloadHasRoom()
        {
            using var testContext = new TestContext();

            testContext.Configuration.Value = new()
            {
                MaxPayloadSize = 25
            };

            testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(
                logLevel:   LogLevel.Information,
                state:      1,
                message:    "This is test event #1"));;
            testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(
                logLevel:   LogLevel.Debug,
                state:      2,
                message:    "This is test event #2"));;

            var eventsSerialized = new List<string>();
            testContext.JsonSerializerOptions.Values.First().Converters.Clear();
            testContext.JsonSerializerOptions.Values.First().Converters.Add(new FakeJsonConverter<SeqLoggerEvent>(
            (writer, value, options) =>
            {
                var serializedEvent = value.BuildMessage();
                writer.WriteStringValue(serializedEvent);
                eventsSerialized.Add(serializedEvent);
            }));

            var uut = testContext.BuildUut();

            using var payloadBuffer = new MemoryStream();
            var payloadEncoding = new UTF8Encoding(false);

            // Trigger a payload overflow, and verify that the overflowed event does not get written.
            {
                var result = uut.AppendPayloadData(payloadBuffer);

                eventsSerialized.Count.ShouldBe(2);

                payloadEncoding.GetString(payloadBuffer.GetBuffer(), 0, (int)payloadBuffer.Length).ShouldBe($"\"{eventsSerialized[0]}\"\n");
                payloadBuffer.Position.ShouldBe(payloadBuffer.Length);

                result.EventsAdded.ShouldBe(1U);
                result.IsDeliveryNeeded.ShouldBeTrue();
            }

            // Verify that the previously overflowed event does not get written, if there's still no room in the buffer.
            {
                var result = uut.AppendPayloadData(payloadBuffer);

                payloadEncoding.GetString(payloadBuffer.GetBuffer(), 0, (int)payloadBuffer.Length).ShouldBe($"\"{eventsSerialized[0]}\"\n");
                payloadBuffer.Position.ShouldBe(payloadBuffer.Length);

                testContext.EventChannel.TryReadEventInvocationCount.ShouldBe(2);

                result.EventsAdded.ShouldBe(0U);
                result.IsDeliveryNeeded.ShouldBeTrue();
            }

            payloadBuffer.SetLength(0);

            // Verify that the previously overflowed event gets written, now that there's room in the buffer.
            {
                var result = uut.AppendPayloadData(payloadBuffer);

                payloadEncoding.GetString(payloadBuffer.GetBuffer(), 0, (int)payloadBuffer.Length).ShouldBe($"\"{eventsSerialized[1]}\"\n");
                payloadBuffer.Position.ShouldBe(payloadBuffer.Length);

                result.EventsAdded.ShouldBe(1U);
                result.IsDeliveryNeeded.ShouldBeFalse();
            }

            payloadBuffer.SetLength(0);
            
            // Verify that the overflowed event is no longer cached.
            {
                var result = uut.AppendPayloadData(payloadBuffer);

                payloadBuffer.Length.ShouldBe(0);
                payloadBuffer.Position.ShouldBe(0);

                result.EventsAdded.ShouldBe(0U);
                result.IsDeliveryNeeded.ShouldBeFalse();
            }
        }

        [Test]
        public void EventSerializationFails_SkipsAndDiscardsEvent()
        {
            using var testContext = new TestContext();

            foreach (var i in Enumerable.Range(1, 10))
                testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(
                    logLevel:   LogLevel.Information,
                    state:      i,
                    message:    $"This is test event #{i}"));

            var eventsSerialized = new List<string>();
            testContext.JsonSerializerOptions.Values.First().Converters.Clear();
            testContext.JsonSerializerOptions.Values.First().Converters.Add(new FakeJsonConverter<SeqLoggerEvent>(
            (writer, value, options) =>
            {
                var serializedEvent = value.BuildMessage();
                if (serializedEvent.Contains("5"))
                    throw new TestException("This is a test exception, simulating a failure to serialize a log event");
                writer.WriteStringValue(serializedEvent);
                eventsSerialized.Add(serializedEvent);
            }));

            var uut = testContext.BuildUut();

            using var payloadBuffer = new MemoryStream();

            // Verify that all events that serialized correctly were written to the payload.
            var result = uut.AppendPayloadData(payloadBuffer);

            var payloadEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            payloadEncoding.GetString(payloadBuffer.GetBuffer(), 0, (int)payloadBuffer.Length).ShouldBe(string.Join("", eventsSerialized.Select(x => $"\"{x}\"\n")));
            payloadBuffer.Position.ShouldBe(payloadBuffer.Length);

            result.EventsAdded.ShouldBe(9U);
            result.IsDeliveryNeeded.ShouldBeFalse();
        }

        [Test]
        public void EventExceedsMaxPayloadSize_SkipsAndDiscardsEvent()
        {
            using var testContext = new TestContext();

            testContext.Configuration.Value = new()
            {
                MaxPayloadSize = 10
            };

            testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(
                logLevel:   LogLevel.Information,
                state:      1,
                message:    "This is test event #1"));;

            var uut = testContext.BuildUut();

            using var payloadBuffer = new MemoryStream();

            var result = uut.AppendPayloadData(payloadBuffer);

            payloadBuffer.Length.ShouldBe(0);
            payloadBuffer.Position.ShouldBe(0);

            result.EventsAdded.ShouldBe(0U);
            result.IsDeliveryNeeded.ShouldBeFalse();
        }
    }
}
