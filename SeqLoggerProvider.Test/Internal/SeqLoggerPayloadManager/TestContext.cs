using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using BatchingLoggerProvider.Internal;
using BatchingLoggerProvider.Utilities;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLoggerPayloadManager;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayloadManager
{
    internal sealed class TestContext
        : IDisposable
    {
        public TestContext()
        {
            var receivedRequestContents = new List<string?>();
            ReceivedRequestContents = receivedRequestContents;

            EventChannel = new FakeBatchingLoggerEventChannel<ISeqLoggerEvent>();

            SystemClock = new();

            HttpMessageHandler = new FakeHttpMessageHandler(async request =>
            {
                SystemClock.Now += HttpResponseDelay;

                receivedRequestContents.Add((request.Content is null)
                    ? null
                    : await request.Content.ReadAsStringAsync());

                return new HttpResponseMessage(HttpResponseStatusCode)
                {
                    Content = new StringContent(HttpResponseMessage ?? string.Empty)
                };
            });

            HttpClientFactory = FakeHttpClientFactory.FromMessageHandler(HttpMessageHandler);

            JsonSerializerOptions = new(new());
            JsonSerializerOptions.Value.Converters.Add(new FakeJsonConverter<ISeqLoggerEvent>(
                (writer, value, options) => writer.WriteStringValue(value.BuildMessage())));

            LoggerFactory = TestLogger.CreateFactory();

            Options = new(new());
        }

        public TimeSpan HttpResponseDelay;

        public string? HttpResponseMessage;

        public HttpStatusCode HttpResponseStatusCode;

        public IReadOnlyList<string?> ReceivedRequestContents;

        public readonly FakeBatchingLoggerEventChannel<ISeqLoggerEvent> EventChannel;

        public readonly FakeHttpClientFactory HttpClientFactory;

        public readonly FakeHttpMessageHandler HttpMessageHandler;

        public readonly FakeOptions<JsonSerializerOptions> JsonSerializerOptions;

        public readonly ILoggerFactory LoggerFactory;

        public readonly FakeOptions<SeqLoggerOptions> Options;

        public readonly FakeSystemClock SystemClock;

        public Uut BuildUut()
            => new(
                eventChannel:           EventChannel,
                httpClientFactory:      HttpClientFactory,
                jsonSerializerOptions:  JsonSerializerOptions,
                logger:                 LoggerFactory.CreateLogger<Uut>(),
                options:                Options,
                systemClock:            SystemClock);

        public void Dispose()
            => LoggerFactory.Dispose();
    }
}
