using System;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.SeqLoggerProvider;

namespace SeqLoggerProvider.Test.SeqLoggerProvider
{
    internal sealed class TestContext
        : IDisposable
    {
        public TestContext()
        {
            EntryChannelWriter      = new();
            EntryPool               = new(() => new FakeSeqLoggerEntry());
            JsonSerializerOptions   = new(new());
            Manager                 = new()
            {
                ShouldStop = true
            };
            Options                 = new(new());
            SelfLogger              = new();
            SystemClock             = new();
        }

        public readonly FakeChannelWriter<ISeqLoggerEntry>  EntryChannelWriter;
        public readonly FakeObjectPool<ISeqLoggerEntry>     EntryPool;
        public readonly FakeOptions<JsonSerializerOptions>  JsonSerializerOptions;
        public readonly FakeSeqLoggerManager                Manager;
        public readonly FakeOptions<SeqLoggerOptions>       Options;
        public readonly TestSeqLoggerSelfLogger             SelfLogger;
        public readonly FakeSystemClock                     SystemClock;

        public Uut BuildUut()
            => new(
                entryChannelWriter:     EntryChannelWriter,
                entryPool:              EntryPool,
                jsonSerializerOptions:  JsonSerializerOptions,
                manager:                Manager,
                options:                Options,
                selfLogger:             SelfLogger,
                systemClock:            SystemClock);

        public void Dispose()
            => SelfLogger.Dispose();
    }
}
