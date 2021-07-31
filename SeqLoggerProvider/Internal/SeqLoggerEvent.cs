using System;
using System.Collections.Generic;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using SeqLoggerProvider.Internal.Json;
using SeqLoggerProvider.Json;

namespace SeqLoggerProvider.Internal
{
    internal abstract class SeqLoggerEvent
    {
        protected SeqLoggerEvent(
            string                  categoryName,
            EventId                 eventId,
            Exception?              exception,
            LogLevel                logLevel,
            DateTimeOffset          occurred,
            IReadOnlyList<object>   scopeStates)
        {
            CategoryName    = categoryName;
            EventId         = eventId;
            Exception       = exception;
            LogLevel        = logLevel;
            Occurred        = occurred;
            ScopeStates     = scopeStates;
        }

        public string CategoryName { get; }

        public EventId EventId { get; }

        public Exception? Exception { get; }

        public LogLevel LogLevel { get; }

        public DateTimeOffset Occurred { get; }

        public IReadOnlyList<object> ScopeStates { get; }

        public abstract string BuildMessage();

        public abstract void WriteState(
            Utf8JsonWriter          writer,
            JsonSerializerOptions   options);
    }

    internal class SeqLoggerEvent<TState>
        : SeqLoggerEvent
    {
        public SeqLoggerEvent(
                string                              categoryName,
                EventId                             eventId,
                Exception?                          exception,
                Func<TState, Exception?, string>    formatter,
                LogLevel                            logLevel,
                DateTimeOffset                      occurred,
                IReadOnlyList<object>               scopeStates,
                TState                              state)
            : base(
                categoryName,
                eventId,
                exception,
                logLevel,
                occurred,
                scopeStates)
        {
            Formatter   = formatter;
            State       = state;
        }

        public Func<TState, Exception?, string> Formatter { get; }

        public TState State { get; }

        public override string BuildMessage()
            => Formatter.Invoke(State, Exception);

        public override void WriteState(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            if (!writer.TryWriteStateFields(State, options)
                && (State is not null))
            {
                var propertyNamingPolicy = options.PropertyNamingPolicy ?? PassthroughJsonNamingPolicy.Default;

                writer.WritePropertyName(propertyNamingPolicy.ConvertName("State"));
                JsonSerializer.Serialize(writer, State, options);
            }
        }
    }
}
