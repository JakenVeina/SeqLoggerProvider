using System;
using System.Threading.Channels;

using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLoggerManager;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerManager
{
    internal sealed class TestContext
        : IDisposable
    {
        public TestContext()
        {
            DeliveryManager     = new();
            EntryChannelReader  = new();
            EntryPool           = new();
            Logger              = new();
            Options             = new(new());
            PayloadPool         = new(() => new FakeSeqLoggerPayload());
            Sequencer           = new(DateTimeOffset.UnixEpoch);
        }

        public readonly FakeSeqLoggerDeliveryManager        DeliveryManager;
        public readonly FakeChannelReader<ISeqLoggerEntry>  EntryChannelReader;
        public readonly FakeObjectPool<ISeqLoggerEntry>     EntryPool;
        public readonly TestSeqLoggerSelfLogger             Logger;
        public readonly FakeOptions<SeqLoggerOptions>       Options;
        public readonly FakeObjectPool<ISeqLoggerPayload>   PayloadPool;
        public readonly FakeSystemClockSequencer            Sequencer;

        public Uut BuildUut()
            => new(
                deliveryManager:    DeliveryManager,
                entryChannelReader: EntryChannelReader,
                entryPool:          EntryPool,
                logger:             Logger,
                options:            Options,
                payloadPool:        PayloadPool,
                systemClock:        Sequencer.SystemClock);

        public void Dispose()
            => Logger.Dispose();
    }
}
