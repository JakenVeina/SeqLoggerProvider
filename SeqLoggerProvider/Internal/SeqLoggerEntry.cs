using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider.Internal
{
    internal interface ISeqLoggerEntry
        : IDisposable
    {
        long BufferLength { get; }

        string CategoryName { get; }

        EventId EventId { get; }

        LogLevel LogLevel { get; }

        DateTime OccurredUtc { get; }

        void CopyBufferTo(Stream destination);

        void Load<TState>(
            string                                  categoryName,
            EventId                                 eventId,
            Exception?                              exception,
            Func<TState, Exception?, string>        formatter,
            IReadOnlyDictionary<string, string>?    globalFields,
            LogLevel                                logLevel,
            DateTime                                occurredUtc,
            IExternalScopeProvider                  scopeProvider,
            TState                                  state,
            JsonSerializerOptions                   options);

        void Reset();
    }

    internal class SeqLoggerEntry
        : ISeqLoggerEntry
    {
        public SeqLoggerEntry()
        {
            _categoryName   = string.Empty;
            _eventId        = new(default, string.Empty);

            _dataBuffer             = new();
            _scopeStatesBuffer      = new();
            _usedFieldNamesBuffer   = new();
        }
        public long BufferLength
            => _dataBuffer.Length;

        public string CategoryName
            => _categoryName;

        public EventId EventId
            => _eventId;

        public LogLevel LogLevel
            => _logLevel;

        public DateTime OccurredUtc
            => _occurredUtc;

        public void CopyBufferTo(Stream destination)
        {
            _dataBuffer.Position = 0;
            _dataBuffer.CopyTo(destination);
        }

        public void Dispose()
            => _dataBuffer.Dispose();

        public void Load<TState>(
            string                                  categoryName,
            EventId                                 eventId,
            Exception?                              exception,
            Func<TState, Exception?, string>        formatter,
            IReadOnlyDictionary<string, string>?    globalFields,
            LogLevel                                logLevel,
            DateTime                                occurredUtc,
            IExternalScopeProvider                  scopeProvider,
            TState                                  state,
            JsonSerializerOptions                   options)
        {
            _dataBuffer.Position = 0;

            var propertyNamingPolicy = options.PropertyNamingPolicy ?? PassthroughJsonNamingPolicy.Default;

            using var writer = new Utf8JsonWriter(_dataBuffer, new()
            {
                Encoder         = options.Encoder,
                Indented        = options.WriteIndented,
                SkipValidation  = true
            });

            writer.WriteStartObject();

            writer.WriteString("@t", occurredUtc.ToString("o", CultureInfo.InvariantCulture));
            
            writer.WriteString("@l", logLevel.ToString());

            if (eventId.Id is not 0)
                writer.WriteNumber("@i", (uint)eventId.Id);

            var message = formatter.Invoke(state, exception);
            if (!string.IsNullOrWhiteSpace(message))
                writer.WriteString("@m", message);

            if (exception is not null)
                writer.WriteString("@x", exception.ToString());

            if (!string.IsNullOrWhiteSpace(categoryName))
                writer.WriteString(propertyNamingPolicy.ConvertName("CategoryName"), categoryName);

            if (!string.IsNullOrWhiteSpace(eventId.Name))
                writer.WriteString(propertyNamingPolicy.ConvertName("EventName"), eventId.Name);

            // Write state fields in order of highest-precedence, to lowest, skipping any fields that have already been written.
            {
                _usedFieldNamesBuffer.Clear();

                var needToWriteState = state is not null;
                if (state is IReadOnlyList<KeyValuePair<string, object?>> fieldset)
                {
                    WriteFieldset(writer, fieldset, options);
                    needToWriteState = false;
                }

                _scopeStatesBuffer.Clear();
                scopeProvider.ForEachScope(
                    static (scopeState, captures) =>
                    {
                        if (scopeState is IReadOnlyList<KeyValuePair<string, object?>> fieldset)
                            captures.@this.WriteFieldset(captures.writer, fieldset, captures.options);
                        else if (scopeState is not null)
                            captures.@this._scopeStatesBuffer.Add(scopeState);
                    },
                    (writer, options, @this: this));

                if (globalFields is not null)
                    foreach (var field in globalFields)
                    {
                        var fieldName = propertyNamingPolicy.ConvertName(field.Key);
                        if (_usedFieldNamesBuffer.Add(fieldName))
                            writer.WriteString(fieldName, field.Value);
                    }

                // Write any state objects that weren't structured as fieldsets into an arbitrary array.
                if (needToWriteState || (_scopeStatesBuffer.Count is not 0))
                {
                    writer.WritePropertyName(propertyNamingPolicy.ConvertName("States"));
                    writer.WriteStartArray();

                    if (needToWriteState)
                        JsonSerializer.Serialize(writer, state, options);

                    foreach (var scopeState in _scopeStatesBuffer)
                        JsonSerializer.Serialize(writer, scopeState, options);

                    writer.WriteEndArray();
                }
            }

            writer.WriteEndObject();

            _categoryName   = categoryName;
            _eventId        = eventId;
            _logLevel       = logLevel;
            _occurredUtc    = occurredUtc;
        }

        public void Reset()
        {
            _dataBuffer.SetLength(0);

            _categoryName   = string.Empty;
            _eventId        = new(default, string.Empty);
            _logLevel       = LogLevel.Trace;
            _occurredUtc    = default;
        }

        private void WriteFieldset<T>(
                Utf8JsonWriter          writer,
                T                       fieldset,
                JsonSerializerOptions   options)
            where T : IReadOnlyList<KeyValuePair<string, object?>>
        {
            var propertyNamingPolicy = options.PropertyNamingPolicy ?? PassthroughJsonNamingPolicy.Default;

            foreach (var field in fieldset)
            {
                if (field.Key == "{OriginalFormat}")
                    continue;

                var fieldName = propertyNamingPolicy.ConvertName(field.Key);
                if (!_usedFieldNamesBuffer.Add(fieldName))
                    continue;

                writer.WritePropertyName(fieldName);

                if (field.Value is null)
                    writer.WriteNullValue();
                else
                    JsonSerializer.Serialize(writer, field.Value, options);
            }
        }

        private readonly MemoryStream       _dataBuffer;
        private readonly List<object>       _scopeStatesBuffer;
        private readonly HashSet<string>    _usedFieldNamesBuffer;

        private string      _categoryName;
        private EventId     _eventId;
        private LogLevel    _logLevel;
        private DateTime    _occurredUtc;
    }
}
