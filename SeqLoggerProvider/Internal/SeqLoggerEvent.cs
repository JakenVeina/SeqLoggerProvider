using System;
using System.Collections.Generic;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using BatchingLoggerProvider.Internal;

using SeqLoggerProvider.Internal.Json;

namespace SeqLoggerProvider.Internal
{
    public interface ISeqLoggerEvent
        : IBatchingLoggerEvent
    {
        bool IsStateNull { get; }

        bool TryWriteStateAsFieldset(
            Utf8JsonWriter          writer,
            JsonSerializerOptions   options,
            HashSet<string>         usedFieldNames);

        void WriteStateAsValue(
            Utf8JsonWriter          writer,
            JsonSerializerOptions   options);
    }

    internal sealed class SeqLoggerEvent<TState>
        : BatchingLoggerEvent<TState>,
            ISeqLoggerEvent
    {
        public SeqLoggerEvent(
                string                              categoryName,
                EventId                             eventId,
                Exception?                          exception,
                Func<TState, Exception?, string>    formatter,
                LogLevel                            logLevel,
                DateTime                            occurredUtc,
                List<object>                        scopeStatesBuffer,
                TState                              state)
            : base(
                categoryName,
                eventId,
                exception,
                formatter,
                logLevel,
                occurredUtc,
                scopeStatesBuffer,
                state)
        { }

        public bool IsStateNull
            => State is null;

        public bool TryWriteStateAsFieldset(
                Utf8JsonWriter          writer,
                JsonSerializerOptions   options,
                HashSet<string>         usedFieldNames)
            => writer.TryWriteFieldset(State, options, usedFieldNames);

        public void WriteStateAsValue(Utf8JsonWriter writer, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, State, options);
    }
}
