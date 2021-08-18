#pragma warning disable CS1591 // Not a public API

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using BatchingLoggerProvider.Internal;
using BatchingLoggerProvider.Utilities;

namespace SeqLoggerProvider.Internal
{
    public class SeqLogger
        : BatchingLoggerBase<ISeqLoggerEvent>
    {
        internal SeqLogger(
                string                                          categoryName,
                IBatchingLoggerEventChannel<ISeqLoggerEvent>    eventChannel,
                IExternalScopeProvider                          externalScopeProvider,
                Action                                          onLog,
                ISystemClock                                    systemClock)
            : base(
                categoryName,
                eventChannel,
                externalScopeProvider,
                onLog,
                systemClock)
        { }

        protected override ISeqLoggerEvent CreateEvent<TState>(
                string                              categoryName,
                EventId                             eventId,
                Exception?                          exception,
                Func<TState, Exception?, string>    formatter,
                LogLevel                            logLevel,
                DateTime                            occurredUtc,
                List<object>                        scopeStatesBuffer,
                TState                              state)
            => new SeqLoggerEvent<TState>(
                categoryName,
                eventId,
                exception,
                formatter,
                logLevel,
                occurredUtc,
                scopeStatesBuffer,
                state);
    }
}
