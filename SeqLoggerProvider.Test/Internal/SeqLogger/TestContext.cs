using System;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLogger;

namespace SeqLoggerProvider.Test.Internal.SeqLogger
{
    internal sealed class TestContext
        : IDisposable
    {
        public TestContext()
        {
            CategoryName = string.Empty;

            EntryChannelWriter      = new();
            EntryPool               = new(() => new FakeSeqLoggerEntry()
            {
                LoadException = EventLoadException
            });
            JsonSerializerOptions   = new(new());
            Logger                  = new();
            Options                 = new(new());
            ScopeProvider           = new();
            SystemClock             = new();
        }

        public string       CategoryName;
        public Exception?   EventLoadException;

        public readonly FakeChannelWriter<ISeqLoggerEntry>  EntryChannelWriter;
        public readonly FakeObjectPool<ISeqLoggerEntry>     EntryPool;
        public readonly FakeOptions<JsonSerializerOptions>  JsonSerializerOptions;
        public readonly TestSeqLoggerSelfLogger             Logger;
        public readonly FakeOptions<SeqLoggerOptions>       Options;
        public readonly FakeExternalScopeProvider           ScopeProvider;
        public readonly FakeSystemClock                     SystemClock;

        public Uut BuildUut()
            => new(
                categoryName:           CategoryName,
                entryChannelWriter:     EntryChannelWriter,
                entryPool:              EntryPool,
                jsonSerializerOptions:  JsonSerializerOptions,
                logger:                 Logger,
                options:                Options,
                scopeProvider:          ScopeProvider,
                systemClock:            SystemClock);

        public void Dispose()
            => Logger.Dispose();

    }
}
