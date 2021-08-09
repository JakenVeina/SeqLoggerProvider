using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SeqLoggerProvider.Internal
{
    internal interface ISeqLoggerPayloadBuilder
    {
        bool IsPayloadDataAvailable { get; }

        AppendPayloadDataResult AppendPayloadData(MemoryStream payload);

        Task WaitForPayloadDataAsync(CancellationToken cancellationToken);
    }

    internal class SeqLoggerPayloadBuilder
        : ISeqLoggerPayloadBuilder,
            IDisposable
    {
        public SeqLoggerPayloadBuilder(
            IOptionsMonitor<JsonSerializerOptions>  jsonSerializerOptions,
            ILogger<SeqLoggerPayloadBuilder>        logger,
            IOptions<SeqLoggerConfiguration>        seqLoggerConfiguration,
            ISeqLoggerEventChannel                  seqLoggerEventChannel)
        {
            _jsonSerializerOptions  = jsonSerializerOptions;
            _logger                 = logger;
            _seqLoggerConfiguration = seqLoggerConfiguration;
            _seqLoggerEventChannel  = seqLoggerEventChannel;

            _overflowBuffer = new();
        }

        public bool IsPayloadDataAvailable
            => (_seqLoggerEventChannel.EventCount is not 0)
                || (_overflowBuffer.Length is not 0);

        public AppendPayloadDataResult AppendPayloadData(MemoryStream payload)
        {
            var eventsAdded = 0U;
            var maxLevel    = _overflowLevel;
    
            // If there was an entry that we couldn't fit in the payload last time, try adding it now.
            if (_overflowBuffer.Length is not 0)
            {
                // Make sure and do ANOTHER overflow check
                if ((payload.Length + _overflowBuffer.Length) > _seqLoggerConfiguration.Value.MaxPayloadSize)
                    return new()
                    {
                        EventsAdded         = 0,
                        IsDeliveryNeeded    = true
                    };

                _overflowBuffer.Position = 0;
                _overflowBuffer.CopyTo(payload);

                _overflowBuffer.SetLength(0);
                _overflowLevel = null;
                ++eventsAdded;
            }

            // Read all available events from the channel, until we run out, or the buffer fills up
            var lastValidPayloadPosition = payload.Position;
            SeqLoggerEvent? @event;
            while ((@event = _seqLoggerEventChannel.TryReadEvent()) is not null)
            {
                try
                {
                    using (var payloadWriter = new Utf8JsonWriter(payload))
                        JsonSerializer.Serialize(payloadWriter, @event, _jsonSerializerOptions.Get(SeqLoggerConstants.JsonSerializerOptionsName));
                    payload.WriteByte((byte)'\n');

                    var maxPayloadSize = _seqLoggerConfiguration.Value.MaxPayloadSize;

                    // If we went past the max payload size, we need to undo that, and move the truncated data to a temporary buffer, for next time.
                    if (payload.Length > maxPayloadSize)
                    {
                        payload.Position = lastValidPayloadPosition;
                        payload.CopyTo(_overflowBuffer);
                        payload.SetLength(lastValidPayloadPosition);

                        if (_overflowBuffer.Length > maxPayloadSize)
                        {
                            SeqLoggerLoggerMessages.EventTooLarge(_logger, @event, _overflowBuffer.Length, maxPayloadSize);
                            _overflowBuffer.SetLength(0);
                        }
                        else
                            _overflowLevel = @event.LogLevel;

                        break;
                    }

                    maxLevel = (!maxLevel.HasValue || (@event.LogLevel > maxLevel.Value))
                        ? @event.LogLevel
                        : maxLevel;

                    ++eventsAdded;
                    lastValidPayloadPosition = payload.Position;
                }
                catch (Exception ex)
                {
                    payload.SetLength(lastValidPayloadPosition);

                    SeqLoggerLoggerMessages.EventSerializationFailed(_logger, ex, @event);
                }
                finally
                {
                    _seqLoggerEventChannel.ReturnScopeStatesBuffer(@event.ScopeStatesBuffer);
                }
            }

            return new()
            {
                EventsAdded         = eventsAdded,
                IsDeliveryNeeded    = (_overflowBuffer.Length is not 0)
                    || (maxLevel.HasValue && (maxLevel.Value >= _seqLoggerConfiguration.Value.PriorityDeliveryLevel))
            };
        }

        public void Dispose()
            => _overflowBuffer.Dispose();

        public Task WaitForPayloadDataAsync(CancellationToken cancellationToken)
            => (_overflowBuffer.Length is not 0)
                ? Task.CompletedTask
                : _seqLoggerEventChannel.WaitForAvailableEventsAsync(cancellationToken);

        private readonly IOptionsMonitor<JsonSerializerOptions> _jsonSerializerOptions;
        private readonly ILogger                                _logger;
        private readonly MemoryStream                           _overflowBuffer;
        private readonly IOptions<SeqLoggerConfiguration>       _seqLoggerConfiguration;
        private readonly ISeqLoggerEventChannel                 _seqLoggerEventChannel;

        private LogLevel? _overflowLevel;
    }
}
