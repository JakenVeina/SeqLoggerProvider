using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SeqLoggerProvider.Utilities;

namespace SeqLoggerProvider.Internal
{
    internal interface ISeqLoggerEventDeliveryManager
    {
        Task<bool> TryDeliverAsync(
            MemoryStream    payload,
            uint            eventCount);
    }

    internal sealed class SeqLoggerEventDeliveryManager
        : ISeqLoggerEventDeliveryManager
    {
        public SeqLoggerEventDeliveryManager(
            IHttpClientFactory                      httpClientFactory,
            ILogger<SeqLoggerEventDeliveryManager>  logger,
            IOptions<SeqLoggerConfiguration>        seqLoggerConfiguration,
            ISystemClock                            systemClock)
        {
            _httpClientFactory      = httpClientFactory;
            _logger                 = logger;
            _seqLoggerConfiguration = seqLoggerConfiguration;
            _systemClock            = systemClock;
        }

        public async Task<bool> TryDeliverAsync(
            MemoryStream    payload,
            uint            eventCount)
        {
            using var httpClient = _httpClientFactory.CreateClient(SeqLoggerConstants.HttpClientName);

            var configuration = _seqLoggerConfiguration.Value;

            httpClient.BaseAddress = new Uri(configuration.ServerUrl);

            var content = new StreamContent(payload); // Not disposing, since there's no leaveOpen option

            content.Headers.ContentType = PayloadContentType;

            if (configuration.ApiKey is not null)
                content.Headers.Add(SeqLoggerConstants.ApiKeyHeaderName, configuration.ApiKey);

            var deliveryStarted = _systemClock.Now;
            SeqLoggerLoggerMessages.EventDeliveryStarting(_logger, eventCount, payload.Length);
            var response = await httpClient.PostAsync(SeqLoggerConstants.EventIngestionApiPath, content);
            var deliveryDuration = _systemClock.Now - deliveryStarted;
            if (!response.IsSuccessStatusCode)
            {
                SeqLoggerLoggerMessages.EventDeliveryFailed(
                    logger:             _logger,
                    serverUrl:          configuration.ServerUrl,
                    statusCode:         response.StatusCode,
                    response:           await response.Content.ReadAsStringAsync(),
                    deliveryDuration:   deliveryDuration);
                return false;
            }
            SeqLoggerLoggerMessages.EventDeliveryFinished(_logger, eventCount, payload.Length, deliveryDuration);

            return true;
        }

        private static readonly MediaTypeHeaderValue PayloadContentType
            = new(SeqLoggerConstants.PayloadMediaType);

        private readonly IHttpClientFactory                 _httpClientFactory;
        private readonly ILogger                            _logger;
        private readonly IOptions<SeqLoggerConfiguration>   _seqLoggerConfiguration;
        private readonly ISystemClock                       _systemClock;
    }
}
