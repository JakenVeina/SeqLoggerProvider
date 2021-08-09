using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Test.Extensions.SeqLoggerProvider.Internal;
using SeqLoggerProvider.Test.Extensions.SeqLoggerProvider.Utilities;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerManager
{
    [TestFixture]
    public class RunAsync
    {
        [Test]
        public async Task IsAlreadyRunning_ThrowsException()
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            var operation = uut.RunAsync(stopTokenSource.Token);

            await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                using var stopTokenSource2 = new CancellationTokenSource();
                stopTokenSource2.Cancel();

                await uut.RunAsync(stopTokenSource2.Token);
            });

            stopTokenSource.Cancel();

            await operation;
        }

        [Test]
        public async Task PayloadIsEmpty_DoesNotDeliverPayload()
        {
            using var testContext = new TestContext();

            var dataset = new PayloadDataset()
            {
                Data            = Array.Empty<byte>(),
                AppendResult    = new()
                {
                    EventsAdded         = 0,
                    IsDeliveryNeeded    = true
                }
            };

            testContext.PayloadBuilder.AddPayloadDataset(dataset);

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            var operation = uut.RunAsync(stopTokenSource.Token);

            stopTokenSource.Cancel();

            await operation;

            testContext.EventDeliveryManager.Deliveries.ShouldBeEmpty();
        }

        [Test]
        public async Task PayloadNeedsDelivery_DeliversPayloadAfterMinDeliveryInterval()
        {
            using var testContext = new TestContext();

            var dataset1 = new PayloadDataset()
            {
                Data            = new byte[] { 1, 2, 3, 4, 5 },
                AppendResult    = new()
                {
                    EventsAdded         = 5,
                    IsDeliveryNeeded    = true
                }
            };
            var dataset2 = new PayloadDataset()
            {
                Data            = new byte[] { 6, 7, 8, 9, 10 },
                AppendResult    = new()
                {
                    EventsAdded         = 5,
                    IsDeliveryNeeded    = true
                }
            };

            testContext.PayloadBuilder.AddPayloadDataset(dataset1);

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            var operation = uut.RunAsync(stopTokenSource.Token);

            await testContext.Sequencer
                // Verify the first delivery, which always occurs immediately
                .DoAfter(TimeSpan.Zero, () =>
                {
                    var delivery = testContext.EventDeliveryManager.Deliveries.ShouldHaveSingleItem();

                    delivery.Data.ShouldBe(dataset1.Data, ignoreOrder: false);
                    delivery.EventCount.ShouldBe(dataset1.AppendResult.EventsAdded);

                    testContext.EventDeliveryManager.ClearDeliveries();
                })
                // Setup the next payload, requesting immediate delivery.
                .DoAfter(TimeSpan.Zero, () => testContext.PayloadBuilder.AddPayloadDataset(dataset2))
                // Verify that the next payload was not delivered immediately, since it hasn't been long enough since the last one.
                .DoAfter(TimeSpan.Zero, () => testContext.EventDeliveryManager.Deliveries.ShouldBeEmpty())
                // Verify delivery after the min interval
                .DoAfter(testContext.Configuration.Value.MinDeliveryInterval, () =>
                {
                    var delivery = testContext.EventDeliveryManager.Deliveries.ShouldHaveSingleItem();

                    delivery.Data.ShouldBe(dataset2.Data, ignoreOrder: false);
                    delivery.EventCount.ShouldBe(dataset2.AppendResult.EventsAdded);
                })
                .RunAsync();

            stopTokenSource.Cancel();

            await operation;
        }

        [Test]
        public async Task PayloadDoesNotNeedDelivery_DeliversPayloadAfterMaxDeliveryInterval()
        {
            using var testContext = new TestContext();

            var dataset1 = new PayloadDataset()
            {
                Data            = new byte[] { 1, 2, 3, 4, 5 },
                AppendResult    = new()
                {
                    EventsAdded         = 5,
                    IsDeliveryNeeded    = false
                }
            };
            var dataset2 = new PayloadDataset()
            {
                Data            = new byte[] { 6, 7, 8, 9, 10 },
                AppendResult    = new()
                {
                    EventsAdded         = 5,
                    IsDeliveryNeeded    = false
                }
            };
            var dataset3 = new PayloadDataset()
            {
                Data            = new byte[] { 11, 12, 13, 14, 15 },
                AppendResult    = new()
                {
                    EventsAdded         = 5,
                    IsDeliveryNeeded    = false
                }
            };

            testContext.PayloadBuilder.AddPayloadDataset(dataset1);

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            var operation = uut.RunAsync(stopTokenSource.Token);

            await testContext.Sequencer
                // Verify the first delivery, which always occurs immediately
                .DoAfter(TimeSpan.Zero, () =>
                {
                    var delivery = testContext.EventDeliveryManager.Deliveries.ShouldHaveSingleItem();

                    delivery.Data.ShouldBe(dataset1.Data, ignoreOrder: false);
                    delivery.EventCount.ShouldBe(dataset1.AppendResult.EventsAdded);

                    testContext.EventDeliveryManager.ClearDeliveries();
                })
                // Setup more payload data, not requiring immediate delivery.
                .DoAfter(TimeSpan.Zero, () => testContext.PayloadBuilder.AddPayloadDataset(dataset2))
                // Verify that the next payload was not delivered immediately, since it wasn't requested
                .DoAfter(TimeSpan.Zero, () => testContext.EventDeliveryManager.Deliveries.ShouldBeEmpty())
                // Setup future payloads, not requiring immediate delivery.
                .DoAfter(TimeSpan.FromSeconds(1), () => testContext.PayloadBuilder.AddPayloadDataset(dataset3))
                // Verify that the next payload was not delivered immediately, since it wasn't requested, and the max interval still hasn't passed.
                .DoAfter(TimeSpan.Zero, () => testContext.EventDeliveryManager.Deliveries.ShouldBeEmpty())
                // Verify that all the data was delivered as a single payload, after waiting the max interval since the previous delivery.
                .DoAfter(testContext.Configuration.Value.MaxDeliveryInterval, () =>
                {
                    var delivery = testContext.EventDeliveryManager.Deliveries.ShouldHaveSingleItem();

                    delivery.Data.ShouldBe(dataset2.Data.Concat(dataset3.Data), ignoreOrder: false);
                    delivery.EventCount.ShouldBe(dataset2.AppendResult.EventsAdded + dataset3.AppendResult.EventsAdded);
                })
                .RunAsync();

            stopTokenSource.Cancel();

            await operation;
        }

        [Test]
        public async Task DeliveryDoesNotSucceed_DoesNotClearPayload()
        {
            using var testContext = new TestContext();

            var dataset1 = new PayloadDataset()
            {
                Data            = new byte[] { 1, 2, 3, 4, 5 },
                AppendResult    = new()
                {
                    EventsAdded         = 5,
                    IsDeliveryNeeded    = true
                }
            };
            var dataset2 = new PayloadDataset()
            {
                Data            = new byte[] { 6, 7, 8, 9, 10 },
                AppendResult    = new()
                {
                    EventsAdded         = 5,
                    IsDeliveryNeeded    = true
                }
            };

            testContext.PayloadBuilder.AddPayloadDataset(dataset1);
            testContext.PayloadBuilder.AddPayloadDataset(dataset2);

            testContext.EventDeliveryManager.CanDeliver = false;

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            var operation = uut.RunAsync(stopTokenSource.Token);

            await testContext.Sequencer
                // After the first delivery fails, set them up to succeed again
                .DoAfter(TimeSpan.Zero, () => testContext.EventDeliveryManager.CanDeliver = true)
                // Verify that the second delivery contains both datasets
                .DoAfter(testContext.Configuration.Value.MinDeliveryInterval, () =>
                {
                    var delivery = testContext.EventDeliveryManager.Deliveries.ShouldHaveSingleItem();

                    delivery.Data.ShouldBe(dataset1.Data.Concat(dataset2.Data), ignoreOrder: false);
                    delivery.EventCount.ShouldBe(dataset1.AppendResult.EventsAdded + dataset2.AppendResult.EventsAdded);
                })
                .RunAsync();

            stopTokenSource.Cancel();

            await operation;
        }

        [Test]
        public async Task StopIsRequested_StopsWaitingAndDeliversRemainingEvents()
        {
            using var testContext = new TestContext();

            var dataset1 = new PayloadDataset()
            {
                Data            = new byte[] { 1, 2, 3, 4, 5 },
                AppendResult    = new()
                {
                    EventsAdded         = 5,
                    IsDeliveryNeeded    = false
                }
            };
            var dataset2 = new PayloadDataset()
            {
                Data            = new byte[] { 6, 7, 8, 9, 10 },
                AppendResult    = new()
                {
                    EventsAdded         = 5,
                    IsDeliveryNeeded    = false
                }
            };
            var dataset3 = new PayloadDataset()
            {
                Data            = new byte[] { 11, 12, 13, 14, 15 },
                AppendResult    = new()
                {
                    EventsAdded         = 5,
                    IsDeliveryNeeded    = false
                }
            };

            testContext.PayloadBuilder.AddPayloadDataset(dataset1);

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            var operation = uut.RunAsync(stopTokenSource.Token);

            await testContext.Sequencer
                // Verify the first delivery, which always occurs immediately
                .DoAfter(TimeSpan.Zero, () =>
                {
                    var delivery = testContext.EventDeliveryManager.Deliveries.ShouldHaveSingleItem();

                    delivery.Data.ShouldBe(dataset1.Data, ignoreOrder: false);
                    delivery.EventCount.ShouldBe(dataset1.AppendResult.EventsAdded);

                    testContext.EventDeliveryManager.ClearDeliveries();
                })
                // Setup a new dataset that will not deliver immediately
                .DoAfter(TimeSpan.Zero, () => testContext.PayloadBuilder.AddPayloadDataset(dataset2))
                // Request to stop, which should trigger an attempt to deliver the pending payload (which must wait the min interval first)
                .DoAfter(TimeSpan.Zero, () => stopTokenSource.Cancel())
                // Also, setup additional data to deliver, while the manager is waiting
                .DoAfter(TimeSpan.Zero, () => testContext.PayloadBuilder.AddPayloadDataset(dataset3))
                // Verify the second delivery, after the minimum interval 
                .DoAfter(testContext.Configuration.Value.MinDeliveryInterval, () =>
                {
                    var delivery = testContext.EventDeliveryManager.Deliveries.ShouldHaveSingleItem();

                    delivery.Data.ShouldBe(dataset2.Data, ignoreOrder: false);
                    delivery.EventCount.ShouldBe(dataset2.AppendResult.EventsAdded);

                    testContext.EventDeliveryManager.ClearDeliveries();
                })
                // Verify the third delivery, after another minimum interval 
                .DoAfter(testContext.Configuration.Value.MinDeliveryInterval, () =>
                {
                    var delivery = testContext.EventDeliveryManager.Deliveries.ShouldHaveSingleItem();

                    delivery.Data.ShouldBe(dataset3.Data, ignoreOrder: false);
                    delivery.EventCount.ShouldBe(dataset3.AppendResult.EventsAdded);
                })
                .RunAsync();

            await operation;
        }
    }
}
