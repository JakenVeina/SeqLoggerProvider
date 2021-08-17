using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayloadManager
{
    [TestFixture]
    public class  WaitForAvailableDataAsync
    {
        [Test]
        public async Task ChannelHasEvents_CompletesImmediately()
        {
            using var testContext = new TestContext();

            testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create<object?>(
                state: default));

            using var uut = testContext.BuildUut();

            uut.IsDataAvailable.ShouldBeTrue();

            var result = uut.WaitForAvailableDataAsync(CancellationToken.None);

            result.IsCompleted.ShouldBeTrue();

            await result;
        }

        [Test]
        public async Task LastPayloadOverflowed_CompletesImmediately()
        {
            using var testContext = new TestContext();

            testContext.Options.Value = new()
            {
                MaxPayloadSize = 25
            };

            foreach (var i in Enumerable.Range(1, 10))
                testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(
                    state:      i,
                    message:    $"This is test event #{i}"));

            using var uut = testContext.BuildUut();

            // Trigger a payload overflow
            uut.TryAppendAvailableDataToPayload();

            // Make sure the result is true because of the overflowed data.
            testContext.EventChannel.ClearEvents();

            uut.IsDataAvailable.ShouldBeTrue();

            var result = uut.WaitForAvailableDataAsync(CancellationToken.None);

            result.IsCompleted.ShouldBeTrue();

            await result;
        }

        [Test]
        public async Task Otherwise_WaitsForAvailableEventsInChannel()
        {
            using var testContext = new TestContext();

            using var uut = testContext.BuildUut();

            uut.IsDataAvailable.ShouldBeFalse();

            var result = uut.WaitForAvailableDataAsync(CancellationToken.None);
            
            result.IsCompleted.ShouldBeFalse();
            
            // Add some events to trigger the wait to complete.
            foreach (var i in Enumerable.Range(1, 10))
                testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(
                    state:      i,
                    message:    $"This is test event #{i}"));

            // Don't deadlock the test if the UUT is broken
            await Task.WhenAny(
                result,
                Task.Delay(TimeSpan.FromSeconds(1)));

            result.IsCompleted.ShouldBeTrue();

            await result;

            // Clear events, so we can make sure the manager goes back into a waiting state
            uut.TryAppendAvailableDataToPayload();
            testContext.EventChannel.ClearEvents();

            uut.IsDataAvailable.ShouldBeFalse();

            result = uut.WaitForAvailableDataAsync(CancellationToken.None);

            result.IsCompleted.ShouldBeFalse();

            testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create<object?>(null));

            // Don't deadlock the test if the UUT is broken
            await Task.WhenAny(
                result,
                Task.Delay(TimeSpan.FromSeconds(1)));

            result.IsCompleted.ShouldBeTrue();

            await result;
        }
    }
}
