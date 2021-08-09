using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLoggerPayloadBuilder;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayloadBuilder
{
    internal sealed class TestContext
        : IDisposable
    {
        public TestContext()
        {
            JsonSerializerOptions = new()
            {
                [SeqLoggerConstants.JsonSerializerOptionsName] = new()
            };
            JsonSerializerOptions.Values.First().Converters.Add(new FakeJsonConverter<SeqLoggerEvent>(
                (writer, value, options) => writer.WriteStringValue(value.BuildMessage())));

            LoggerFactory = TestLogger.CreateFactory();

            Configuration = new(new());

            EventChannel = new();
        }

        public readonly FakeOptionsMonitor<JsonSerializerOptions> JsonSerializerOptions;

        public readonly ILoggerFactory LoggerFactory;

        public readonly FakeOptions<SeqLoggerConfiguration> Configuration;

        public readonly FakeSeqLoggerEventChannel EventChannel;

        public Uut BuildUut()
            => new(
                jsonSerializerOptions:  JsonSerializerOptions,
                logger:                 LoggerFactory.CreateLogger<Uut>(),
                seqLoggerConfiguration: Configuration,
                seqLoggerEventChannel:  EventChannel);

        public void Dispose()
            => LoggerFactory.Dispose();
    }
}
