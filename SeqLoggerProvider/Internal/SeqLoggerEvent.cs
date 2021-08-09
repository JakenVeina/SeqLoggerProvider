using System;
using System.Collections.Generic;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using SeqLoggerProvider.Internal.Json;

namespace SeqLoggerProvider.Internal
{
    internal abstract class SeqLoggerEvent
    {
        protected SeqLoggerEvent(
            string          categoryName,
            EventId         eventId,
            Exception?      exception,
            LogLevel        logLevel,
            DateTime        occurredUtc,
            List<object>    scopeStatesBuffer)
        {
            CategoryName       = categoryName;
            EventId            = eventId;
            Exception          = exception;
            LogLevel           = logLevel;
            OccurredUtc        = occurredUtc;
            ScopeStatesBuffer  = scopeStatesBuffer;
        }

        public string CategoryName { get; }

        public EventId EventId { get; }

        public Exception? Exception { get; }

        public abstract bool IsStateNull { get; }

        public LogLevel LogLevel { get; }

        public DateTime OccurredUtc { get; }

        public List<object> ScopeStatesBuffer { get; }

        public abstract string BuildMessage();

        public abstract bool TryWriteStateAsFieldset(
            Utf8JsonWriter          writer,
            JsonSerializerOptions   options,
            HashSet<string>         usedFieldNames);

        public abstract void WriteStateAsValue(
            Utf8JsonWriter          writer,
            JsonSerializerOptions   options);
    }

    internal sealed class SeqLoggerEvent<TState>
        : SeqLoggerEvent
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
                logLevel,
                occurredUtc,
                scopeStatesBuffer)
        {
            Formatter   = formatter;
            State       = state;
        }

        public Func<TState, Exception?, string> Formatter { get; }

        public override bool IsStateNull
            => State is null;

        public TState State { get; }

        public override string BuildMessage()
            => Formatter.Invoke(State, Exception);

        public override bool TryWriteStateAsFieldset(
                Utf8JsonWriter          writer,
                JsonSerializerOptions   options,
                HashSet<string>         usedFieldNames)
            => writer.TryWriteFieldset(State, options, usedFieldNames);

        public override void WriteStateAsValue(Utf8JsonWriter writer, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, State, options);
    }
}
