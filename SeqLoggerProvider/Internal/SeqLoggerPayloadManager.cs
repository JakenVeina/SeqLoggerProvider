using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using BatchingLoggerProvider.Internal;
using BatchingLoggerProvider.Utilities;

namespace SeqLoggerProvider.Internal
{
    internal sealed class SeqLoggerPayloadManager
        : IBatchingLoggerPayloadManager,
            IDisposable
    {
        public SeqLoggerPayloadManager(
            IBatchingLoggerEventChannel<ISeqLoggerEvent>    eventChannel,
            IHttpClientFactory                              httpClientFactory,
            IOptions<JsonSerializerOptions>                 jsonSerializerOptions,
            ILogger<SeqLoggerPayloadManager>                logger,
            IOptions<SeqLoggerOptions>                      options,
            ISystemClock                                    systemClock)
        {
            _eventChannel           = eventChannel;
            _httpClientFactory      = httpClientFactory;
            _jsonSerializerOptions  = jsonSerializerOptions;
            _logger                 = logger;
            _options                = options;
            _systemClock            = systemClock;

            _overflowBuffer = new();
            _payloadBuffer  = new();
        }

        public bool IsDataAvailable
            => (_eventChannel.EventCount is not 0)
                || (_overflowBuffer.Length is not 0);

        public bool IsDeliveryNeeded
            => _isDeliveryNeeded;

        public bool IsPayloadEmpty
            => _payloadBuffer.Length is 0;

        public uint PayloadEventCount
            => _payloadEventCount;

        public void Dispose()
        {
            _overflowBuffer.Dispose();
            _payloadBuffer.Dispose();
        }

        public void TryAppendAvailableDataToPayload()
        {
            // If there was an entry that we couldn't fit in the payload last time, try adding it now.
            if (_overflowBuffer.Length is not 0)
            {
                // Make sure and do ANOTHER overflow check
                if ((_payloadBuffer.Length + _overflowBuffer.Length) > _options.Value.MaxPayloadSize)
                {
                    _isDeliveryNeeded = true;
                    return;
                }

                _overflowBuffer.Position = 0;
                _overflowBuffer.CopyTo(_payloadBuffer);
                _overflowBuffer.SetLength(0);

                if (_overflowLevel.HasValue && (_overflowLevel.Value >= _options.Value.PriorityDeliveryLevel))
                    _isDeliveryNeeded = true;
                _overflowLevel = null;

                ++_payloadEventCount;
            }

            // Read all available events from the channel, until we run out, or the buffer fills up
            var lastValidPayloadPosition = _payloadBuffer.Position;
            ISeqLoggerEvent? @event;
            while ((@event = _eventChannel.TryReadEvent()) is not null)
            {
                try
                {
                    using (var payloadWriter = new Utf8JsonWriter(_payloadBuffer))
                        JsonSerializer.Serialize(payloadWriter, @event, _jsonSerializerOptions.Value);
                    _payloadBuffer.WriteByte((byte)'\n');

                    var maxPayloadSize = _options.Value.MaxPayloadSize;

                    // If we went past the max payload size, we need to undo that, and move the truncated data to a temporary buffer, for next time.
                    if (_payloadBuffer.Length > maxPayloadSize)
                    {
                        _payloadBuffer.Position = lastValidPayloadPosition;
                        _payloadBuffer.CopyTo(_overflowBuffer);
                        _payloadBuffer.SetLength(lastValidPayloadPosition);

                        if (_overflowBuffer.Length > maxPayloadSize)
                        {
                            SeqLoggerLoggerMessages.EventTooLarge(_logger, @event, _overflowBuffer.Length, maxPayloadSize);
                            _overflowBuffer.SetLength(0);
                        }
                        else
                        {
                            _overflowLevel = @event.LogLevel;
                            _isDeliveryNeeded = true;
                        }

                        break;
                    }

                    if (@event.LogLevel >= _options.Value.PriorityDeliveryLevel)
                        _isDeliveryNeeded = true;

                    ++_payloadEventCount;
                    lastValidPayloadPosition = _payloadBuffer.Position;
                }
                catch (Exception ex)
                {
                    _payloadBuffer.SetLength(lastValidPayloadPosition);

                    SeqLoggerLoggerMessages.EventSerializationFailed(_logger, ex, @event);
                }
                finally
                {
                    @event.ReturnTo(_eventChannel);
                }
            }
        }

        public async Task TryDeliverPayloadAsync()
        {
            using var httpClient = _httpClientFactory.CreateClient(SeqLoggerConstants.HttpClientName);

            var options = _options.Value;

            httpClient.BaseAddress = new Uri(options.ServerUrl);

            _payloadBuffer.Position = 0;
            var content = new StreamContent(_payloadBuffer); // Not disposing, since there's no leaveOpen option

            content.Headers.ContentType = PayloadContentType;

            if (options.ApiKey is not null)
                content.Headers.Add(SeqLoggerConstants.ApiKeyHeaderName, options.ApiKey);

            var deliveryStarted = _systemClock.Now;

            SeqLoggerLoggerMessages.EventDeliveryStarting(_logger, _payloadEventCount, _payloadBuffer.Length);

            try
            {
                using var response = await httpClient.PostAsync(SeqLoggerConstants.EventIngestionApiPath, content);
                var deliveryDuration = _systemClock.Now - deliveryStarted;

                if (!response.IsSuccessStatusCode)
                {
                    SeqLoggerLoggerMessages.EventDeliveryFailed(
                        logger:             _logger,
                        serverUrl:          options.ServerUrl,
                        statusCode:         response.StatusCode,
                        response:           await response.Content.ReadAsStringAsync(),
                        exception:          null,
                        deliveryDuration:   deliveryDuration);
                    return;
                }

                SeqLoggerLoggerMessages.EventDeliveryFinished(_logger, _payloadEventCount, _payloadBuffer.Length, deliveryDuration);
            }
            catch (Exception ex)
            {
                var deliveryDuration = _systemClock.Now - deliveryStarted;
                SeqLoggerLoggerMessages.EventDeliveryFailed(
                    logger:             _logger,
                    serverUrl:          options.ServerUrl,
                    statusCode:         null,
                    response:           null,
                    exception:          ex,
                    deliveryDuration:   deliveryDuration);
                return;
            }

            _payloadBuffer.SetLength(0);
            _payloadEventCount = 0;
            _isDeliveryNeeded = false;
        }

        public Task WaitForAvailableDataAsync(CancellationToken cancellationToken)
            => (_overflowBuffer.Length is not 0)
                ? Task.CompletedTask
                : _eventChannel.WaitForAvailableEventsAsync(cancellationToken);

        private static readonly MediaTypeHeaderValue PayloadContentType
            = new(SeqLoggerConstants.PayloadMediaType);

        private readonly IBatchingLoggerEventChannel<ISeqLoggerEvent>   _eventChannel;
        private readonly IHttpClientFactory                             _httpClientFactory;
        private readonly IOptions<JsonSerializerOptions>                _jsonSerializerOptions;
        private readonly ILogger                                        _logger;
        private readonly MemoryStream                                   _overflowBuffer;
        private readonly IOptions<SeqLoggerOptions>                     _options;
        private readonly MemoryStream                                   _payloadBuffer;
        private readonly ISystemClock                                   _systemClock;

        private bool        _isDeliveryNeeded;
        private LogLevel?   _overflowLevel;
        private uint        _payloadEventCount;
    }
}
