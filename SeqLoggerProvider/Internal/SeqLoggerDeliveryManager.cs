using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace SeqLoggerProvider.Internal
{
    internal interface ISeqLoggerDeliveryManager
    {
        Task DeliverAsync(ISeqLoggerPayload payload);
    }

    internal class SeqLoggerDeliveryManager
        : ISeqLoggerDeliveryManager
    {
        public SeqLoggerDeliveryManager(
            IHttpClientFactory          httpClientFactory,
            ISeqLoggerSelfLogger        logger,
            IOptions<SeqLoggerOptions>  options,
            ISystemClock                systemClock)
        {
            _httpClientFactory  = httpClientFactory;
            _logger             = logger;
            _options            = options;
            _systemClock        = systemClock;
        }

        public async Task DeliverAsync(ISeqLoggerPayload payload)
        {
            using var httpClient = _httpClientFactory.CreateClient(SeqLoggerConstants.HttpClientName);

            var options = _options.Value;

            httpClient.BaseAddress = new Uri(options.ServerUrl);

            payload.Buffer.Position = 0;
            var content = new StreamContent(payload.Buffer); // Not disposing, since there's no leaveOpen option

            content.Headers.ContentType = PayloadContentType;

            if (options.ApiKey is not null)
                content.Headers.Add(SeqLoggerConstants.ApiKeyHeaderName, options.ApiKey);

            var deliveryStarted = _systemClock.Now;

            SeqLoggerLoggerMessages.DeliveryStarting(_logger, payload);

            try
            {
                using var response = await httpClient.PostAsync(SeqLoggerConstants.EventIngestionApiPath, content);
                var deliveryDuration = _systemClock.Now - deliveryStarted;

                if (response.IsSuccessStatusCode)
                    SeqLoggerLoggerMessages.DeliveryFinished(_logger, payload, deliveryDuration);
                else
                    SeqLoggerLoggerMessages.DeliveryFailed(
                        logger:             _logger,
                        serverUrl:          options.ServerUrl,
                        statusCode:         response.StatusCode,
                        response:           await response.Content.ReadAsStringAsync(),
                        exception:          null,
                        deliveryDuration:   deliveryDuration);
            }
            catch (Exception ex)
            {
                var deliveryDuration = _systemClock.Now - deliveryStarted;
                SeqLoggerLoggerMessages.DeliveryFailed(
                    logger:             _logger,
                    serverUrl:          options.ServerUrl,
                    statusCode:         null,
                    response:           null,
                    exception:          ex,
                    deliveryDuration:   deliveryDuration);
                return;
            }
        }

        private static readonly MediaTypeHeaderValue PayloadContentType
            = new(SeqLoggerConstants.PayloadMediaType);

        private readonly IHttpClientFactory         _httpClientFactory;
        private readonly ISeqLoggerSelfLogger       _logger;
        private readonly IOptions<SeqLoggerOptions> _options;
        private readonly ISystemClock               _systemClock;
    }
}
