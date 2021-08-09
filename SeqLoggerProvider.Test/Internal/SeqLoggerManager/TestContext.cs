using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SeqLoggerProvider.Test.Extensions.SeqLoggerProvider.Internal;
using SeqLoggerProvider.Test.Extensions.SeqLoggerProvider.Utilities;

using Uut = SeqLoggerProvider.Internal.SeqLoggerManager;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerManager
{
    internal sealed class TestContext
        : IDisposable
    {
        public TestContext()
        {
            LoggerFactory = TestLogger.CreateFactory();

            Configuration = new(new());

            EventDeliveryManager = new();

            PayloadBuilder = new();

            Sequencer = new(DateTimeOffset.UnixEpoch);
        }

        public readonly ILoggerFactory LoggerFactory;

        public readonly FakeOptions<SeqLoggerConfiguration> Configuration;

        public readonly FakeSeqLoggerEventDeliveryManager EventDeliveryManager;

        public readonly FakeSeqLoggerPayloadBuilder PayloadBuilder;

        public readonly FakeSystemClockSequencer Sequencer;

        public Uut BuildUut()
            => new(
                logger:                         LoggerFactory.CreateLogger<Uut>(),
                seqLoggerConfiguration:         Configuration,
                seqLoggerEventDeliveryManager:  EventDeliveryManager,
                seqLoggerPayloadBuilder:        PayloadBuilder,
                systemClock:                    Sequencer.SystemClock);

        public void Dispose()
            => LoggerFactory.Dispose();
    }
}
