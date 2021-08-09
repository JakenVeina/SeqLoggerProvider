using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SeqLoggerProvider.Utilities;

namespace SeqLoggerProvider.Internal
{
    internal interface ISeqLoggerManager
    {
        Task RunAsync(CancellationToken cancellationToken);
    }

    internal class SeqLoggerManager
        : ISeqLoggerManager
    {
        public SeqLoggerManager(
            ILogger<SeqLoggerManager>           logger,
            IOptions<SeqLoggerConfiguration>    seqLoggerConfiguration,
            ISeqLoggerEventDeliveryManager      seqLoggerEventDeliveryManager,
            ISeqLoggerPayloadBuilder            seqLoggerPayloadBuilder,
            ISystemClock                        systemClock)
        {
            _logger                         = logger;
            _seqLoggerConfiguration         = seqLoggerConfiguration;
            _seqLoggerEventDeliveryManager  = seqLoggerEventDeliveryManager;
            _seqLoggerPayloadBuilder        = seqLoggerPayloadBuilder;
            _systemClock                    = systemClock;
        }

        public async Task RunAsync(CancellationToken stopToken)
        {
            if (_isRunning)
                throw new InvalidOperationException("The manager is already running");
            _isRunning = true;

            SeqLoggerLoggerMessages.ManagerStarted(_logger);

            try
            {
                using var payload = new MemoryStream(_seqLoggerConfiguration.Value.MaxPayloadSize);

                var payloadEventCount = 0U;
                var lastDelivery = default(DateTimeOffset?);

                // When stop is requested, don't stop immediately. Make sure we flush all available events first
                while (!stopToken.IsCancellationRequested || _seqLoggerPayloadBuilder.IsPayloadDataAvailable)
                {
                    try
                    {
                        // Figure out whether we should wait for more payload data, or go ahead and deliver what we have.
                        // If we're stopping, we don't wait.
                        // If there's already data available, we don't wait.
                        // If we have a pending payload, and it's been too long since the last delivery, we don't wait.
                        if (!stopToken.IsCancellationRequested && !_seqLoggerPayloadBuilder.IsPayloadDataAvailable)
                        {
                            if (lastDelivery.HasValue)
                            {
                                if (payloadEventCount is 0)
                                    await _seqLoggerPayloadBuilder.WaitForPayloadDataAsync(stopToken);
                                else
                                {
                                    var remainingInterval = _seqLoggerConfiguration.Value.MaxDeliveryInterval - (_systemClock.Now - lastDelivery.Value);
                                    // If MaxDeliveryInterval has been exceeded, we actually skip this and exit this "wait" block entirely
                                    if (remainingInterval >= TimeSpan.Zero)
                                        await Task.WhenAny(
                                            _seqLoggerPayloadBuilder.WaitForPayloadDataAsync(stopToken),
                                            _systemClock.WaitAsync(remainingInterval, stopToken));
                                }

                            }
                            else
                                await _seqLoggerPayloadBuilder.WaitForPayloadDataAsync(stopToken);
                        }
                    }
                    catch (OperationCanceledException) { }

                    // If there's actually no data processing to do, there's no point continuing this iteration.
                    // This can happen when stopToken is triggered while we're waiting in the above block, in which case the loop is about to terminate
                    if ((payloadEventCount is 0) && !_seqLoggerPayloadBuilder.IsPayloadDataAvailable)
                        continue;

                    var appendPayloadDataResult = _seqLoggerPayloadBuilder.AppendPayloadData(payload);
                    payloadEventCount += appendPayloadDataResult.EventsAdded;

                    // One last empty-payload check, before we consider delivering it (can happen if there are errors in the builder)
                    if (payloadEventCount is 0)
                        continue;

                    // If this is the first delivery, just do it immediately.
                    if (lastDelivery.HasValue)
                    {
                        var elapsedSinceLastDelivery = _systemClock.Now - lastDelivery.Value;

                        // Only send the payload when we're in the middle of stopping, when the builder reports that it's needed, or when it's been too long since the payload was started
                        if (!stopToken.IsCancellationRequested
                                && !appendPayloadDataResult.IsDeliveryNeeded
                                && (elapsedSinceLastDelivery < _seqLoggerConfiguration.Value.MaxDeliveryInterval))
                            continue;

                        // If we've decided to deliver what we have, make sure we wait at least the minimum interval since the last one.
                        if (lastDelivery.HasValue)
                        {
                            var remainingInterval = _seqLoggerConfiguration.Value.MinDeliveryInterval - (_systemClock.Now - lastDelivery.Value);
                            if (remainingInterval > TimeSpan.Zero)
                                await _systemClock.WaitAsync(remainingInterval, CancellationToken.None);
                        }
                    }

                    payload.Position = 0;
                    if (await _seqLoggerEventDeliveryManager.TryDeliverAsync(payload, payloadEventCount))
                    {
                        payload.SetLength(0);
                        payloadEventCount = 0;
                    }
                    else
                        payload.Position = payload.Length;
                    lastDelivery = _systemClock.Now;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                SeqLoggerLoggerMessages.ManagerCrashed(_logger, ex);
                throw;
            }

            SeqLoggerLoggerMessages.ManagerStopped(_logger);
            _isRunning = false;
        }

        private readonly ILogger                            _logger;
        private readonly IOptions<SeqLoggerConfiguration>   _seqLoggerConfiguration;
        private readonly ISeqLoggerEventDeliveryManager     _seqLoggerEventDeliveryManager;
        private readonly ISeqLoggerPayloadBuilder           _seqLoggerPayloadBuilder;
        private readonly ISystemClock                       _systemClock;

        private bool _isRunning;
    }
}
