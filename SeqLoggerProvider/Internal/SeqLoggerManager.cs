using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
            IHttpClientFactory                      httpClientFactory,
            IOptionsMonitor<JsonSerializerOptions>  jsonSerializerOptions,
            ILogger<SeqLoggerManager>               logger,
            IOptions<SeqLoggerConfiguration>        seqLoggerConfiguration,
            ISeqLoggerEventChannel                  seqLoggerEventChannel,
            ISystemClock                            systemClock)
        {
            _httpClientFactory      = httpClientFactory;
            _jsonSerializerOptions  = jsonSerializerOptions;
            _logger                 = logger;
            _seqLoggerConfiguration = seqLoggerConfiguration;
            _seqLoggerEventChannel  = seqLoggerEventChannel;
            _systemClock            = systemClock;
        }

        public async Task RunAsync(CancellationToken stopToken)
        {
            SeqLoggerLoggerMessages.ManagerStarted(_logger);

            try
            {
                using var payloadBuffer = new MemoryStream(_seqLoggerConfiguration.Value.MaxPayloadSize);
                using var truncatedEventBuffer = new MemoryStream(1000);

                var payloadEventCount = 0;
                var lastBatchSent = _systemClock.UtcNow;
                var maxLogLevelInBatch = LogLevel.Trace;

                while (!stopToken.IsCancellationRequested)
                {
                    // If there was an entry that we couldn't fit in the payload last time, try adding it now.
                    if ((truncatedEventBuffer.Length is not 0)
                        && ((payloadBuffer.Length + truncatedEventBuffer.Length) <= _seqLoggerConfiguration.Value.MaxPayloadSize))
                    {
                        truncatedEventBuffer.CopyTo(payloadBuffer);
                        truncatedEventBuffer.SetLength(0);
                        ++payloadEventCount;
                    }

                    // Wait either until more events are available, or, if there's a pending payload to be sent, until we hit the max interval for sending payloads
                    var remainingIntervalUntilMax = _seqLoggerConfiguration.Value.MaxBatchInterval - (_systemClock.UtcNow - lastBatchSent);
                    if ((payloadBuffer.Length is not 0)
                            && (remainingIntervalUntilMax > TimeSpan.Zero))
                        await _seqLoggerEventChannel.WaitForAvailableEventsAsync(stopToken);
                    else
                        await Task.WhenAny(
                            _seqLoggerEventChannel.WaitForAvailableEventsAsync(stopToken),
                            Task.Delay(remainingIntervalUntilMax, stopToken));

                    // Read all available events from the channel, until we run out, or the buffer fills up
                    var lastValidPayloadPosition = payloadBuffer.Position;
                    while (_seqLoggerEventChannel.TryReadEvent(out var @event)
                        && (truncatedEventBuffer.Length is 0))
                    {
                        try
                        {
                            using (var payloadWriter = new Utf8JsonWriter(payloadBuffer))
                                JsonSerializer.Serialize(payloadWriter, @event, _jsonSerializerOptions.Get(SeqLoggerConstants.JsonSerializerOptionsName));

                            // bufferSize cannot be omitted, pulled from https://github.com/dotnet/runtime/blob/2abd4878e2e356389352a909baa15043399cd0ca/src/libraries/System.Private.CoreLib/src/System/IO/StreamWriter.cs#L25
                            using (var payloadWriter = new StreamWriter(payloadBuffer, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
                                payloadWriter.WriteLine();

                            maxLogLevelInBatch = (@event.LogLevel > maxLogLevelInBatch)
                                ? @event.LogLevel
                                : maxLogLevelInBatch;

                            // If we went past the max payload size, move it over to another temp buffer, for next time.
                            if (payloadBuffer.Length > _seqLoggerConfiguration.Value.MaxPayloadSize)
                            {
                                payloadBuffer.Position = lastValidPayloadPosition;
                                payloadBuffer.CopyTo(truncatedEventBuffer);
                                payloadBuffer.SetLength(lastValidPayloadPosition);
                                break;
                            }

                            ++payloadEventCount;
                            lastValidPayloadPosition = payloadBuffer.Position;
                        }
                        catch (Exception ex)
                        {
                            payloadBuffer.SetLength(lastValidPayloadPosition);

                            SeqLoggerLoggerMessages.EventSerializationFailed(_logger, ex, @event);
                        }
                    }

                    // Only send a batch if the buffer is full, if we've waited too long, or if the batch contains any errors.
                    var elapsedSinceLastBatch = _systemClock.UtcNow - lastBatchSent;
                    if ((truncatedEventBuffer.Length is 0)
                            && (elapsedSinceLastBatch <= _seqLoggerConfiguration.Value.MaxBatchInterval)
                            && (maxLogLevelInBatch < LogLevel.Error))
                        continue;

                    // If we've decided to send a batch, make sure we wait at least the minimum interval before sending another.
                    {
                        var remainingIntervalUntilMin = _seqLoggerConfiguration.Value.MinBatchInterval - elapsedSinceLastBatch;
                        if (remainingIntervalUntilMin > TimeSpan.Zero)
                            await Task.Delay(remainingIntervalUntilMin);
                    }

                    using (var httpClient = _httpClientFactory.CreateClient(SeqLoggerConstants.HttpClientName))
                    {
                        var configuration = _seqLoggerConfiguration.Value;

                        httpClient.BaseAddress = new Uri(configuration.ServerUrl);

                        payloadBuffer.Position = 0;
                        var content = new StreamContent(payloadBuffer); // Not disposing, since there's no leaveOpen option

                        content.Headers.ContentType = PayloadContentType;

                        if (configuration.ApiKey is not null)
                            content.Headers.Add("X-Seq-ApiKey", configuration.ApiKey);

                        var deliveryStarted = _systemClock.UtcNow;
                        SeqLoggerLoggerMessages.EventDeliveryStarting(_logger, payloadEventCount, payloadBuffer.Length);
                        var result = await httpClient.PostAsync("api/events/raw", content, stopToken);
                        if (!result.IsSuccessStatusCode)
                        {
                            SeqLoggerLoggerMessages.EventDeliveryFailed(
                                logger:     _logger,
                                serverUrl:  configuration.ServerUrl,
                                statusCode: result.StatusCode,
                                response:   await result.Content.ReadAsStringAsync()); // TODO: Add cancellation if it's ever added to netstandard
                            continue;
                        }
                        SeqLoggerLoggerMessages.EventDeliveryFinished(_logger, payloadEventCount, payloadBuffer.Length, _systemClock.UtcNow - deliveryStarted);
                    }

                    payloadBuffer.SetLength(0);
                    maxLogLevelInBatch = LogLevel.Trace;
                    payloadEventCount = 0;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                SeqLoggerLoggerMessages.ManagerCrashed(_logger, ex);
            }

            SeqLoggerLoggerMessages.ManagerStopped(_logger);
        }

        private static readonly MediaTypeHeaderValue PayloadContentType
            = new(SeqLoggerConstants.PayloadMediaType);

        private readonly IHttpClientFactory                     _httpClientFactory;
        private readonly IOptionsMonitor<JsonSerializerOptions> _jsonSerializerOptions;
        private readonly ILogger                                _logger;
        private readonly IOptions<SeqLoggerConfiguration>       _seqLoggerConfiguration;
        private readonly ISeqLoggerEventChannel                 _seqLoggerEventChannel;
        private readonly ISystemClock                           _systemClock;
    }
}
