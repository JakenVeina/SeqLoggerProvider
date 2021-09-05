using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerManager
{
    [TestFixture]
    public class EnsureStarted
    {
        [Test]
        public async Task ManagerHasStarted_DoesNothing()
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            uut.EnsureStarted(stopTokenSource.Token);

            uut.EnsureStarted(stopTokenSource.Token);

            uut.HasStarted.ShouldBeTrue();
            testContext.EntryChannelReader.WaitCount.ShouldBe(1);

            stopTokenSource.Cancel();

            await uut.WhenStopped;
        }

        public static readonly IReadOnlyList<TestCaseData> PayloadHasPriorityEvent_TestCaseData
            = new[]
            {
                /*                  priorityDeliveryLevel,  minDeliveryInterval,        eventCount  */
                new TestCaseData(   LogLevel.Trace,         TimeSpan.FromSeconds(1),    1           ).SetName("{m}(Single event, Trace level)"),
                new TestCaseData(   LogLevel.Trace,         TimeSpan.FromSeconds(2),    10          ).SetName("{m}(Many events, Trace level)"),
                new TestCaseData(   LogLevel.Debug,         TimeSpan.FromSeconds(3),    10          ).SetName("{m}(Many events, Debug level)"),
                new TestCaseData(   LogLevel.Information,   TimeSpan.FromSeconds(4),    10          ).SetName("{m}(Many events, Information level)"),
                new TestCaseData(   LogLevel.Warning,       TimeSpan.FromSeconds(5),    10          ).SetName("{m}(Many events, Warning level)"),
                new TestCaseData(   LogLevel.Error,         TimeSpan.FromSeconds(6),    10          ).SetName("{m}(Many events, Error level)"),
                new TestCaseData(   LogLevel.Critical,      TimeSpan.FromSeconds(7),    10          ).SetName("{m}(Many events, Critical level)")
            };

        [TestCaseSource(nameof(PayloadHasPriorityEvent_TestCaseData))]
        public async Task PayloadHasPriorityEvent_DeliversPayloadAfterMinInterval(
            LogLevel    priorityDeliveryLevel,
            TimeSpan    minDeliveryInterval,
            int         eventCount)
        {
            using var testContext = new TestContext();

            testContext.Options.Value.PriorityDeliveryLevel = priorityDeliveryLevel;
            testContext.Options.Value.MinDeliveryInterval   = minDeliveryInterval;

            var initialEvent = FakeSeqLoggerEntry.Create();

            var events = FakeSeqLoggerEntry.Generate(
                    entryCount:     eventCount,
                    maxLogLevel:    priorityDeliveryLevel)
                .ToArray();

            testContext.EntryChannelReader.AddItem(initialEvent);

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            uut.EnsureStarted(stopTokenSource.Token);

            uut.HasStarted.ShouldBeTrue();

            await testContext.Sequencer
                // Verify the first delivery, which always occurs immediately
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldHaveSingleItem().ShouldBeSameAs(initialEvent);

                    testContext.EntryPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(initialEvent);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem().ShouldBeSameAs(payload);
                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);

                    testContext.EntryPool.Clear();
                    testContext.PayloadPool.Clear();
                    testContext.DeliveryManager.Clear();
                })
                // Setup the next payload, which should deliver ASAP.
                .DoAfter(TimeSpan.Zero, () => testContext.EntryChannelReader.AddItems(events))
                // Verify that the next payload was prepared, but not delivered yet, since it hasn't been long enough since the last one.
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldBe(events, ignoreOrder: false);

                    testContext.EntryPool.ReturnedObjects.ShouldBe(events, ignoreOrder: true);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldBeEmpty();
                    testContext.PayloadPool.ReturnedObjects.ShouldBeEmpty();

                    testContext.EntryPool.Clear();
                    testContext.PayloadPool.Clear();
                })
                // Verify delivery after the min interval
                .DoAfter(minDeliveryInterval, () =>
                {
                    var payload = testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem();
                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);

                    testContext.EntryPool.ReturnedObjects.ShouldBeEmpty();
                    testContext.PayloadPool.CreatedObjects.ShouldBeEmpty();
                })
                .RunAsync();

            stopTokenSource.Cancel();

            await uut.WhenStopped;
        }

        public static readonly IReadOnlyList<TestCaseData> PayloadDoesNotHavePriorityEvent_TestCaseData
            = new[]
            {
                /*                  priorityDeliveryLevel,  minDeliveryInterval,        maxDeliveryInterval,        eventCount, additionalEventCount,   maxLogLevel             */
                new TestCaseData(   LogLevel.Debug,         TimeSpan.FromSeconds(1),    TimeSpan.FromSeconds(2),    1,          0,                      LogLevel.Trace          ).SetName("{m}(Single event, Trace level)"),
                new TestCaseData(   LogLevel.Debug,         TimeSpan.FromSeconds(3),    TimeSpan.FromSeconds(4),    10,         5,                      LogLevel.Trace          ).SetName("{m}(Many events, Trace level)"),
                new TestCaseData(   LogLevel.Information,   TimeSpan.FromSeconds(5),    TimeSpan.FromSeconds(6),    10,         5,                      LogLevel.Debug          ).SetName("{m}(Many events, Debug level)"),
                new TestCaseData(   LogLevel.Warning,       TimeSpan.FromSeconds(7),    TimeSpan.FromSeconds(8),    10,         5,                      LogLevel.Information    ).SetName("{m}(Many events, Information level)"),
                new TestCaseData(   LogLevel.Error,         TimeSpan.FromSeconds(9),    TimeSpan.FromSeconds(10),   10,         5,                      LogLevel.Warning        ).SetName("{m}(Many events, Warning level)"),
                new TestCaseData(   LogLevel.Critical,      TimeSpan.FromSeconds(11),   TimeSpan.FromSeconds(12),   10,         5,                      LogLevel.Error          ).SetName("{m}(Many events, Error level)"),
                new TestCaseData(   LogLevel.None,          TimeSpan.FromSeconds(13),   TimeSpan.FromSeconds(14),   10,         5,                      LogLevel.Critical       ).SetName("{m}(Many events, Critical level)")
            };

        [TestCaseSource(nameof(PayloadDoesNotHavePriorityEvent_TestCaseData))]
        public async Task PayloadDoesNotHavePriorityEvent_DeliversPayloadAfterMaxInterval(
            LogLevel    priorityDeliveryLevel,
            TimeSpan    minDeliveryInterval,
            TimeSpan    maxDeliveryInterval,
            int         eventCount,
            int         additionalEventCount,
            LogLevel    maxLogLevel)
        {
            using var testContext = new TestContext();

            testContext.Options.Value.PriorityDeliveryLevel = priorityDeliveryLevel;
            testContext.Options.Value.MaxDeliveryInterval   = maxDeliveryInterval;
            testContext.Options.Value.MinDeliveryInterval   = minDeliveryInterval;

            var initialEvent = FakeSeqLoggerEntry.Create();

            var events = FakeSeqLoggerEntry.Generate(
                    entryCount:     eventCount,
                    maxLogLevel:    maxLogLevel)
                .ToArray();

            var additionalEvents = FakeSeqLoggerEntry.Generate(
                    entryCount:     additionalEventCount,
                    maxLogLevel:    maxLogLevel)
                .ToArray();

            testContext.EntryChannelReader.AddItem(initialEvent);

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            uut.EnsureStarted(stopTokenSource.Token);

            uut.HasStarted.ShouldBeTrue();

            await testContext.Sequencer
                // Verify the first delivery, which always occurs immediately
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldHaveSingleItem().ShouldBeSameAs(initialEvent);

                    testContext.EntryPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(initialEvent);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem().ShouldBeSameAs(payload);
                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);

                    testContext.EntryPool.Clear();
                    testContext.PayloadPool.Clear();
                    testContext.DeliveryManager.Clear();
                })
                // Setup the next payload, which should not trigger immediate delivery.
                .DoAfter(TimeSpan.Zero, () => testContext.EntryChannelReader.AddItems(events))
                // Verify that the next payload was prepped, but not delivered
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldBe(events, ignoreOrder: false);

                    testContext.EntryPool.ReturnedObjects.ShouldBe(events, ignoreOrder: true);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldBeEmpty();
                    testContext.PayloadPool.ReturnedObjects.ShouldBeEmpty();

                    testContext.EntryPool.Clear();
                })
                // Setup additional events, also not requiring immediate delivery.
                .DoAfter(TimeSpan.FromSeconds(1), () => testContext.EntryChannelReader.AddItems(additionalEvents))
                // Verify that the additional events were added to the existing payload.
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldBe(events.Concat(additionalEvents), ignoreOrder: false);

                    testContext.EntryPool.ReturnedObjects.ShouldBe(additionalEvents, ignoreOrder: true);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldBeEmpty();
                    testContext.PayloadPool.ReturnedObjects.ShouldBeEmpty();

                    testContext.EntryPool.Clear();
                    testContext.PayloadPool.Clear();
                })
                // Verify that the payload is finally delivered, after waiting the max interval since the previous delivery.
                .DoAfter(testContext.Options.Value.MaxDeliveryInterval - TimeSpan.FromSeconds(1), () =>
                {
                    var payload = testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem();
                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);

                    testContext.EntryPool.ReturnedObjects.ShouldBeEmpty();
                    testContext.PayloadPool.CreatedObjects.ShouldBeEmpty();
                })
                .RunAsync();

            stopTokenSource.Cancel();

            await uut.WhenStopped;
        }

        public static readonly IReadOnlyList<TestCaseData> PayloadCannotFitEvent_TestCaseData
            = new[]
            {
                /*                  minDeliveryInterval,        maxDeliveryInterval,        maxPayloadSize, entryCount, entrySize,  firstPayloadEntryCount  */
                new TestCaseData(   TimeSpan.FromSeconds(1),    TimeSpan.FromSeconds(2),    1U,             2,          1L,         1                       ).SetName("{m}(Trivial sizes)"),
                new TestCaseData(   TimeSpan.FromSeconds(3),    TimeSpan.FromSeconds(4),    4U,             3,          2L,         2                       ).SetName("{m}(Payload is full, single event overflows)"),
                new TestCaseData(   TimeSpan.FromSeconds(3),    TimeSpan.FromSeconds(4),    6U,             5,          2L,         3                       ).SetName("{m}(Payload is full, many events overflow)"),
                new TestCaseData(   TimeSpan.FromSeconds(5),    TimeSpan.FromSeconds(6),    10U,            4,          3L,         3                       ).SetName("{m}(Payload is not full, single event overflows)"),
                new TestCaseData(   TimeSpan.FromSeconds(7),    TimeSpan.FromSeconds(8),    15U,            4,          7L,         2                       ).SetName("{m}(Payload is not full, many events overflow)")
            };

        [TestCaseSource(nameof(PayloadCannotFitEvent_TestCaseData))]
        public async Task PayloadCannotFitEvent_CachesEventAndDeliversPayload(
            TimeSpan    minDeliveryInterval,
            TimeSpan    maxDeliveryInterval,
            uint        maxPayloadSize,
            int         entryCount,
            long        entrySize,
            int         firstPayloadEntryCount)
        {
            using var testContext = new TestContext();

            testContext.Options.Value.PriorityDeliveryLevel = LogLevel.None;
            testContext.Options.Value.MinDeliveryInterval   = minDeliveryInterval;
            testContext.Options.Value.MaxDeliveryInterval   = maxDeliveryInterval;
            testContext.Options.Value.MaxPayloadSize        = maxPayloadSize;

            var initialEntry = FakeSeqLoggerEntry.Create();

            var entries = FakeSeqLoggerEntry.Generate(
                    entryCount:     entryCount,
                    bufferLength:   entrySize)
                .ToArray();

            var firstPayloadEntries = entries.Take(firstPayloadEntryCount)
                .ToArray();

            var secondPayloadEntries = entries.Skip(firstPayloadEntryCount)
                .ToArray();

            testContext.EntryChannelReader.AddItem(initialEntry);

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            uut.EnsureStarted(stopTokenSource.Token);

            uut.HasStarted.ShouldBeTrue();

            await testContext.Sequencer
                // Verify the first delivery, which always occurs immediately
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldHaveSingleItem().ShouldBeSameAs(initialEntry);

                    testContext.EntryPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(initialEntry);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem().ShouldBeSameAs(payload);
                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);

                    testContext.EntryPool.Clear();
                    testContext.PayloadPool.Clear();
                    testContext.DeliveryManager.Clear();
                })
                // Setup the next payload, which should NOT contain all the given events, and should thus deliver ASAP
                .DoAfter(TimeSpan.Zero, () => testContext.EntryChannelReader.AddItems(entries))
                // Verify that the next payload was prepared, but not delivered yet, since it hasn't been long enough since the last one.
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.Count.ShouldBe(secondPayloadEntries.Length - 1);

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldBe(firstPayloadEntries, ignoreOrder: false);

                    testContext.EntryPool.ReturnedObjects.ShouldBe(firstPayloadEntries, ignoreOrder: true);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldBeEmpty();
                    testContext.PayloadPool.ReturnedObjects.ShouldBeEmpty();

                    testContext.EntryPool.Clear();
                    testContext.PayloadPool.Clear();
                })
                // Verify delivery after the min interval
                .DoAfter(minDeliveryInterval, () =>
                {
                    var payload = testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem();
                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);

                    testContext.DeliveryManager.Clear();
                })
                // Verify that the remaining events that couldn't fit in the first payload are prepared for the next.
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldBe(secondPayloadEntries, ignoreOrder: false);

                    testContext.EntryPool.ReturnedObjects.ShouldBe(secondPayloadEntries, ignoreOrder: true);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldBeEmpty();
                    testContext.PayloadPool.ReturnedObjects.ShouldNotContain(payload);

                    testContext.EntryPool.Clear();
                    testContext.PayloadPool.Clear();
                })
                // Verify delivery after the max interval
                .DoAfter(maxDeliveryInterval, () =>
                {
                    var payload = testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem();
                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);

                    testContext.EntryPool.ReturnedObjects.ShouldBeEmpty();
                    testContext.PayloadPool.CreatedObjects.ShouldBeEmpty();
                })
                .RunAsync();

            stopTokenSource.Cancel();

            await uut.WhenStopped;
        }

        public static readonly IReadOnlyList<TestCaseData> EventIsTooLarge_TestCaseData
            = new[]
            {
                /*                  maxPayloadSize, entrySize       */
                new TestCaseData(   0U,             1L              ).SetName("{m}(MaxPayloadSize is 0)"),
                new TestCaseData(   1U,             2L              ).SetName("{m}(MaxPayloadSize is 1)"),
                new TestCaseData(   10U,            11L             ).SetName("{m}(MaxPayloadSize is 10)"),
                new TestCaseData(   uint.MaxValue,  long.MaxValue   ).SetName("{m}(MaxPayloadSize is MaxValue)")
            };

        [TestCaseSource(nameof(EventIsTooLarge_TestCaseData))]
        public async Task EventIsTooLarge_IgnoresEvent(
            uint maxPayloadSize,
            long entrySize)
        {
            using var testContext = new TestContext();

            testContext.Options.Value.MaxPayloadSize = maxPayloadSize;

            var initialEntry = FakeSeqLoggerEntry.Create(
                bufferLength:   entrySize);

            testContext.EntryChannelReader.AddItem(initialEntry);

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            uut.EnsureStarted(stopTokenSource.Token);

            uut.HasStarted.ShouldBeTrue();

            await testContext.Sequencer
                // Verify the initial event was ignored, and no deliveries or payload preparation has occurred.
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    testContext.EntryPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(initialEntry);

                    testContext.PayloadPool.CreatedObjects.ShouldBeEmpty();
                    testContext.DeliveryManager.DeliveredPayloads.ShouldBeEmpty();
                    testContext.PayloadPool.ReturnedObjects.ShouldBeEmpty();
                })
                .RunAsync();

            stopTokenSource.Cancel();

            await uut.WhenStopped;
        }

        [Test]
        public async Task CachedEventIsTooLarge_IgnoresEvent()
        {
            using var testContext = new TestContext();

            testContext.DeliveryManager.ShouldCompleteDeliveries = false;

            testContext.Options.Value.PriorityDeliveryLevel = LogLevel.None;
            testContext.Options.Value.MaxPayloadSize        = 11;

            var entries = FakeSeqLoggerEntry.Generate(2, bufferLength: 10)
                .ToArray();

            testContext.EntryChannelReader.AddItems(entries);

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            uut.EnsureStarted(stopTokenSource.Token);

            uut.HasStarted.ShouldBeTrue();

            await testContext.Sequencer
                // Verify only the first event was delivered, since the second event is too big for the payload.
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();

                    payload.Entries.ShouldHaveSingleItem().ShouldBeSameAs(entries[0]);
                    testContext.EntryPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(entries[0]);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem().ShouldBeSameAs(payload);
                    testContext.PayloadPool.ReturnedObjects.ShouldBeEmpty();

                    testContext.EntryPool.Clear();
                    testContext.DeliveryManager.Clear();
                })
                // While the manager is waiting on the delivery to complete, change the payload size
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.Options.Value.MaxPayloadSize = 9;
                    testContext.DeliveryManager.ShouldCompleteDeliveries = true;
                })
                // Verify only the first event was delivered, since the second event is too big for the payload.
                .DoAfter(TimeSpan.Zero, () =>
                {
                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem();
                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldBeEmpty();

                    testContext.EntryPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(entries[1]);
                })
                .RunAsync();

            stopTokenSource.Cancel();

            await uut.WhenStopped;
        }

        [Test]
        public async Task StopIsRequested_StopsWaitingAndDeliversRemainingEvents()
        {
            using var testContext = new TestContext();

            var entries = FakeSeqLoggerEntry.Generate(entryCount: 3)
                .ToArray();

            testContext.EntryChannelReader.AddItem(entries[0]);

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            uut.EnsureStarted(stopTokenSource.Token);

            await testContext.Sequencer
                // Verify the first delivery, which always occurs immediately
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldHaveSingleItem().ShouldBeSameAs(entries[0]);

                    testContext.EntryPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(entries[0]);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem().ShouldBeSameAs(payload);
                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);

                    testContext.EntryPool.Clear();
                    testContext.PayloadPool.Clear();
                    testContext.DeliveryManager.Clear();
                })
                // Setup another event that will not deliver immediately
                .DoAfter(TimeSpan.Zero, () => testContext.EntryChannelReader.AddItem(entries[1]))
                // Request to stop, which should trigger an attempt to deliver the pending payload (which must wait the min interval first)
                .DoAfter(TimeSpan.Zero, () => stopTokenSource.Cancel())
                // Also, setup an additional event to deliver, while the manager is waiting
                .DoAfter(TimeSpan.Zero, () => testContext.EntryChannelReader.AddItem(entries[2]))
                // Verify that the second event has been processed, but the third has not, since the manager is still waiting to deliver the second.
                .DoAfter(TimeSpan.Zero, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldHaveSingleItem().ShouldBeSameAs(entries[2]);

                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldHaveSingleItem().ShouldBeSameAs(entries[1]);

                    testContext.EntryPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(entries[1]);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldBeEmpty();
                    testContext.PayloadPool.ReturnedObjects.ShouldBeEmpty();

                    testContext.EntryPool.Clear();
                    testContext.PayloadPool.Clear();
                })
                // Verify the second delivery, after the minimum interval
                .DoAfter(testContext.Options.Value.MinDeliveryInterval, () =>
                {
                    testContext.EntryChannelReader.Items.ShouldBeEmpty();

                    var payload = testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldHaveSingleItem().ShouldBeSameAs(entries[1]);

                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);

                    testContext.DeliveryManager.Clear();
                })
                // Verify the third event being prepared, but not delivered, since a delivery just happened.
                .DoAfter(TimeSpan.Zero, () =>
                {
                    var payload = testContext.PayloadPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldHaveSingleItem().ShouldBeSameAs(entries[2]);

                    testContext.EntryPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(entries[2]);

                    testContext.DeliveryManager.DeliveredPayloads.ShouldBeEmpty();
                    testContext.PayloadPool.ReturnedObjects.ShouldNotContain(payload);

                    testContext.EntryPool.Clear();
                    testContext.PayloadPool.Clear();
                    testContext.DeliveryManager.Clear();
                })
                // Verify the third delivery, after another minimum interval 
                .DoAfter(testContext.Options.Value.MinDeliveryInterval, () =>
                {
                    var payload = testContext.DeliveryManager.DeliveredPayloads.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerPayload>();
                    payload.Entries.ShouldHaveSingleItem().ShouldBeSameAs(entries[2]);

                    testContext.PayloadPool.ReturnedObjects.ShouldHaveSingleItem().ShouldBeSameAs(payload);
                })
                .RunAsync();

            await uut.WhenStopped;
        }
    }
}
