using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayloadManager
{
    [TestFixture]
    public class TryAppendAvailableDataToPayload
    {
        public static IReadOnlyList<TestCaseData> ChannelHasEvents_TestCaseData()
            => new[]
            {
                /*                  priorityDeliveryLevel,  isDeliveryNeeded,   logEventLevels                                                                                                      */
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
            bool                    isDeliveryNeeded,
            IReadOnlyList<LogLevel> eventLogLevels)
        {
            using var testContext = new TestContext();

            testContext.Options.Value = new()
            {
                PriorityDeliveryLevel = priorityDeliveryLevel
            };

            foreach (var i in Enumerable.Range(1, eventLogLevels.Count))
                testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(
                    logLevel:   eventLogLevels[i - 1],
                    state:      i,
                    message:    $"This is test event #{i}"));;

            var eventsSerialized = new List<string>();
            testContext.JsonSerializerOptions.Value.Converters.Clear();
            testContext.JsonSerializerOptions.Value.Converters.Add(new FakeJsonConverter<ISeqLoggerEvent>(
            (writer, value, options) =>
            {
                var serializedEvent = value.BuildMessage();
                writer.WriteStringValue(serializedEvent);
                eventsSerialized.Add(serializedEvent);
            }));

            using var uut = testContext.BuildUut();

            uut.TryAppendAvailableDataToPayload();

            eventsSerialized.Count.ShouldBe(eventLogLevels.Count);

            uut.IsPayloadEmpty.ShouldBeFalse();
            uut.IsDeliveryNeeded.ShouldBe(isDeliveryNeeded);
            uut.PayloadEventCount.ShouldBe((uint)eventLogLevels.Count);
        }

        [Test]
        public async Task PayloadOverflows_CachesEventUntilPayloadHasRoom()
        {
            using var testContext = new TestContext()
            {
                HttpResponseStatusCode = HttpStatusCode.OK
            };

            testContext.Options.Value = new()
            {
                MaxPayloadSize  = 25,
                ServerUrl       = "http://localhost"
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
            testContext.JsonSerializerOptions.Value.Converters.Clear();
            testContext.JsonSerializerOptions.Value.Converters.Add(new FakeJsonConverter<ISeqLoggerEvent>(
            (writer, value, options) =>
            {
                var serializedEvent = value.BuildMessage();
                writer.WriteStringValue(serializedEvent);
                eventsSerialized.Add(serializedEvent);
            }));

            using var uut = testContext.BuildUut();

            // Trigger a payload overflow, and verify that the overflowed event does not get written.
            {
                uut.TryAppendAvailableDataToPayload();

                eventsSerialized.Count.ShouldBe(2);

                uut.IsPayloadEmpty.ShouldBeFalse();
                uut.IsDeliveryNeeded.ShouldBeTrue();
                uut.PayloadEventCount.ShouldBe(1U);
            }

            // Verify that the previously overflowed event does not get written, if there's still no room in the buffer.
            {
                uut.TryAppendAvailableDataToPayload();

                uut.IsPayloadEmpty.ShouldBeFalse();
                uut.IsDeliveryNeeded.ShouldBeTrue();
                uut.PayloadEventCount.ShouldBe(1U);
            }

            // Clear the payload buffer
            await uut.TryDeliverPayloadAsync();

            // Verify that the previously overflowed event gets written, now that there's room in the buffer.
            {
                uut.TryAppendAvailableDataToPayload();

                uut.IsPayloadEmpty.ShouldBeFalse();
                uut.IsDeliveryNeeded.ShouldBeFalse();
                uut.PayloadEventCount.ShouldBe(1U);
            }

            // Clear the payload buffer, again
            await uut.TryDeliverPayloadAsync();

            // Verify that the overflowed event is no longer cached.
            {
                uut.TryAppendAvailableDataToPayload();

                uut.IsPayloadEmpty.ShouldBeTrue();
                uut.IsDeliveryNeeded.ShouldBeFalse();
                uut.PayloadEventCount.ShouldBe(0U);
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
            testContext.JsonSerializerOptions.Value.Converters.Clear();
            testContext.JsonSerializerOptions.Value.Converters.Add(new FakeJsonConverter<ISeqLoggerEvent>(
            (writer, value, options) =>
            {
                var serializedEvent = value.BuildMessage();
                if (serializedEvent.Contains("5"))
                    throw new TestException("This is a test exception, simulating a failure to serialize a log event");
                writer.WriteStringValue(serializedEvent);
                eventsSerialized.Add(serializedEvent);
            }));

            using var uut = testContext.BuildUut();

            // Verify that all events that serialized correctly were written to the payload.
            uut.TryAppendAvailableDataToPayload();

            uut.IsPayloadEmpty.ShouldBeFalse();
            uut.IsDeliveryNeeded.ShouldBeFalse();
            uut.PayloadEventCount.ShouldBe(9U);
        }

        [Test]
        public void EventExceedsMaxPayloadSize_SkipsAndDiscardsEvent()
        {
            using var testContext = new TestContext();

            testContext.Options.Value = new()
            {
                MaxPayloadSize = 10
            };

            testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(
                logLevel:   LogLevel.Information,
                state:      1,
                message:    "This is test event #1"));;

            using var uut = testContext.BuildUut();

            uut.TryAppendAvailableDataToPayload();

            uut.IsPayloadEmpty.ShouldBeTrue();
            uut.IsDeliveryNeeded.ShouldBeFalse();
            uut.PayloadEventCount.ShouldBe(0U);
        }
    }
}
