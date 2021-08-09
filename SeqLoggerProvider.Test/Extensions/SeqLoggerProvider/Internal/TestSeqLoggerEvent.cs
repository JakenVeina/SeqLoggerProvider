using System;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider.Internal
{
    internal static class TestSeqLoggerEvent
    {
        public static SeqLoggerEvent Create<TState>(
                                    TState                  state,
                                    LogLevel                logLevel            = LogLevel.Information,
                                    int                     eventId             = default,
                                    string?                 message             = default,
                                    DateTime?               occurredUtc         = default,
                                    Exception?              exception           = default,
                [CallerFilePath]    string                  callerFilePath      = default!,
                [CallerMemberName]  string                  callerMemberName    = default!)
            => new SeqLoggerEvent<TState>(
                categoryName:       callerFilePath,
                eventId:            new(eventId, callerMemberName),
                exception:          exception,
                formatter:          (_, _) => message ?? "This is a test event",
                logLevel:           logLevel,
                occurredUtc:        occurredUtc ?? DateTimeOffset.UnixEpoch.UtcDateTime,
                scopeStatesBuffer:  new(),
                state:              state);
    }
}
