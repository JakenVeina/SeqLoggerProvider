using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace SeqLoggerProvider.Internal
{
    internal interface ISeqLoggerManager
    {
        bool HasStarted { get; }

        Task WhenStopped { get; }

        void EnsureStarted(CancellationToken stopToken);
    }

    internal class SeqLoggerManager
        : ISeqLoggerManager
    {
        public SeqLoggerManager(
            ISeqLoggerDeliveryManager       deliveryManager,
            ChannelReader<ISeqLoggerEntry>  entryChannelReader,
            ObjectPool<ISeqLoggerEntry>     entryPool,
            ISeqLoggerSelfLogger            logger,
            IOptions<SeqLoggerOptions>      options,
            ObjectPool<ISeqLoggerPayload>   payloadPool,
            ISystemClock                    systemClock)
        {
            Debug.Assert(entryChannelReader.CanCount);

            _deliveryManager    = deliveryManager;
            _entryChannelReader = entryChannelReader;
            _entryPool          = entryPool;
            _logger             = logger;
            _options            = options;
            _payloadPool        = payloadPool;
            _systemClock        = systemClock;

            _whenStoppedSource = new();
        }

        public bool HasStarted
            => _hasStarted is not 0;

        public Task WhenStopped
            => _whenStoppedSource.Task;

        public void EnsureStarted(CancellationToken stopToken)
        {
            var hasStarted = Interlocked.Exchange(ref _hasStarted, 1);
            if (hasStarted is not 0)
                return;

            RunAsync(stopToken);
        }

        private async void RunAsync(CancellationToken stopToken)
        {
            var currentPayload  = default(ISeqLoggerPayload?);
            var currentEntry    = default(ISeqLoggerEntry?);
            try
            {
                SeqLoggerLoggerMessages.ManagerStarted(_logger);

                var lastDelivery    = default(DateTimeOffset?);

                // When stop is requested, don't stop immediately. Make sure we flush all available entries first
                while (!stopToken.IsCancellationRequested || (_entryChannelReader.Count is not 0))
                {
                    var wasPriorityEventAppended = false;

                    // If there's an entry leftover from last time, handle that first.
                    if (currentEntry is not null)
                    {
                        var maxPayloadSize = _options.Value.MaxPayloadSize;
                        if (currentEntry.BufferLength > maxPayloadSize)
                        {
                            SeqLoggerLoggerMessages.EntryTooLarge(_logger, currentEntry, maxPayloadSize);

                            _entryPool.Return(currentEntry);
                            currentEntry = null;
                        }
                        else if ((currentEntry.BufferLength + (currentPayload?.Buffer.Length ?? 0)) <= maxPayloadSize)
                        {
                            if (currentPayload is null)
                                currentPayload = _payloadPool.Get();

                            currentPayload.Append(currentEntry);

                            wasPriorityEventAppended |= currentEntry.LogLevel >= _options.Value.PriorityDeliveryLevel;

                            _entryPool.Return(currentEntry);
                            currentEntry = null;
                        }
                    }
                    else
                    {
                        try
                        {
                            // Figure out whether we should wait for more entries to come in.
                            // If we're stopping, we don't wait.
                            // If there's already entries available, we don't wait.
                            // If we have a pending payload, and it's been too long since the last delivery, we don't wait.
                            if (!stopToken.IsCancellationRequested && (_entryChannelReader.Count is 0))
                            {
                                if (lastDelivery.HasValue)
                                {
                                    if (currentPayload is null)
                                        await _entryChannelReader.WaitToReadAsync(stopToken);
                                    else
                                    {
                                        var remainingInterval = _options.Value.MaxDeliveryInterval - (_systemClock.Now - lastDelivery.Value);
                                        // If MaxDeliveryInterval has been exceeded, exit this "wait" block entirely
                                        if (remainingInterval >= TimeSpan.Zero)
                                            await Task.WhenAny(
                                                _entryChannelReader.WaitToReadAsync(stopToken).AsTask(),
                                                _systemClock.WaitAsync(remainingInterval, stopToken));
                                    }

                                }
                                else
                                    await _entryChannelReader.WaitToReadAsync(stopToken);
                            }
                        }
                        catch (OperationCanceledException) { }
                    }

                    // If there's actually no processing to do, there's no point continuing this iteration.
                    // This can happen when stopToken is triggered while we're waiting in the above block, I.E. during shutdown
                    if ((currentPayload is null) && (_entryChannelReader.Count is 0))
                        continue;

                    // If the payload isn't full, read available entries into it.
                    if (currentEntry is null)
                    {
                        while(_entryChannelReader.TryRead(out currentEntry))
                        {
                            var maxPayloadSize = _options.Value.MaxPayloadSize;
                            if (currentEntry.BufferLength > maxPayloadSize)
                                SeqLoggerLoggerMessages.EntryTooLarge(_logger, currentEntry, maxPayloadSize);
                            else if ((currentEntry.BufferLength + currentPayload?.Buffer.Length) > maxPayloadSize)
                                break;
                            else
                            {
                                if (currentPayload is null)
                                    currentPayload = _payloadPool.Get();

                                currentPayload.Append(currentEntry);
                                wasPriorityEventAppended |= currentEntry.LogLevel >= _options.Value.PriorityDeliveryLevel;
                            }

                            _entryPool.Return(currentEntry);
                            currentEntry = null;
                        }
                    }

                    // This shouldn't be possible, but for the sake of the compiler's null-checks...
                    if (currentPayload is null)
                        continue;

                    // If this is the first delivery, we don't have a prior delivery to do interval calculations against, so just skip them and deliver immediately.
                    if (lastDelivery.HasValue)
                    {
                        var elapsedSinceLastDelivery = _systemClock.Now - lastDelivery.Value;

                        // Send the payload when...
                        //      We've been requested to stop (we need to flush all remaining entries)
                        //      It contains a priority entry
                        //      It's been too long since the last delivery
                        //      The payload has overflowed (I.E. there was an event that couldn't fit)
                        if (!stopToken.IsCancellationRequested
                                && !wasPriorityEventAppended
                                && (currentEntry is null)
                                && (elapsedSinceLastDelivery < _options.Value.MaxDeliveryInterval))
                            continue;

                        // If we've decided to deliver what we have, make sure we wait at least the minimum interval since the last one.
                        if (lastDelivery.HasValue)
                        {
                            var remainingInterval = _options.Value.MinDeliveryInterval - (_systemClock.Now - lastDelivery.Value);
                            if (remainingInterval > TimeSpan.Zero)
                                await _systemClock.WaitAsync(remainingInterval, CancellationToken.None);
                        }
                    }

                    await _deliveryManager.DeliverAsync(currentPayload);
                    lastDelivery = _systemClock.Now;

                    // Recycle the payload (kinda only for testing)
                    _payloadPool.Return(currentPayload);
                    currentPayload = null;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                SeqLoggerLoggerMessages.ManagerCrashed(_logger, ex);
            }
            finally
            {
                if (currentEntry is not null)
                    _entryPool.Return(currentEntry);
                if (currentPayload is not null)
                    _payloadPool.Return(currentPayload);
            }

            _whenStoppedSource.SetResult(null);
            SeqLoggerLoggerMessages.ManagerStopped(_logger);
        }

        private readonly ISeqLoggerDeliveryManager          _deliveryManager;
        private readonly ChannelReader<ISeqLoggerEntry>     _entryChannelReader;
        private readonly ObjectPool<ISeqLoggerEntry>        _entryPool;
        private readonly ISeqLoggerSelfLogger               _logger;
        private readonly IOptions<SeqLoggerOptions>         _options;
        private readonly ObjectPool<ISeqLoggerPayload>      _payloadPool;
        private readonly ISystemClock                       _systemClock;
        private readonly TaskCompletionSource<object?>      _whenStoppedSource;

        private int _hasStarted; // Interlocked does not support bool
    }
}
