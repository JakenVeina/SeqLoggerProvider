using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

using SeqLoggerProvider.Json;

namespace SeqLoggerProvider.Internal.Json
{
    internal class SeqLoggerEventJsonConverter
        : JsonConverter<ISeqLoggerEvent>
    {
        public SeqLoggerEventJsonConverter(IOptions<SeqLoggerOptions> options)
        {
            _options = options;

            _usedFieldNamesPool = ObjectPool.Create(new UsedFieldNamesPooledObjectPolicy());
        }

        public override ISeqLoggerEvent Read(
                ref Utf8JsonReader      reader,
                Type                    typeToConvert,
                JsonSerializerOptions   options)
            => throw new NotSupportedException();

        public override void Write(
            Utf8JsonWriter          writer,
            ISeqLoggerEvent         value,
            JsonSerializerOptions   options)
        {
            var propertyNamingPolicy = options.PropertyNamingPolicy ?? PassthroughJsonNamingPolicy.Default;

            writer.WriteStartObject();

            writer.WriteString("@t", value.OccurredUtc.ToString("o", CultureInfo.InvariantCulture));
            
            writer.WriteString("@l", value.LogLevel.ToString());

            if (value.EventId.Id is not 0)
                writer.WriteNumber("@i", (uint)value.EventId.Id);

            var message = value.BuildMessage();
            if (!string.IsNullOrWhiteSpace(message))
                writer.WriteString("@m", message);

            if (value.Exception is not null)
                writer.WriteString("@x", value.Exception.ToString());

            if (!string.IsNullOrWhiteSpace(value.CategoryName))
                writer.WriteString(propertyNamingPolicy.ConvertName("CategoryName"), value.CategoryName);

            if (!string.IsNullOrWhiteSpace(value.EventId.Name))
                writer.WriteString("EventName", value.EventId.Name);

            // Write state fields in order of highest-precedence, to lowest, skipping any fields that have already been written.
            {
                var usedFieldNames = _usedFieldNamesPool.Get();
                try
                {
                    var needToWriteState = !value.TryWriteStateAsFieldset(writer, options, usedFieldNames) && !value.IsStateNull;

                    var needToWriteAnyScopeStates = false;
                    foreach (var scopeState in value.ScopeStatesBuffer)
                        if (!writer.TryWriteFieldset(scopeState, options, usedFieldNames))
                            needToWriteAnyScopeStates = true;

                    var globalFields = _options.Value.GlobalFields;
                    if (globalFields is not null)
                        foreach (var field in globalFields)
                        {
                            var fieldName = propertyNamingPolicy.ConvertName(field.Key);
                            if (usedFieldNames.Add(fieldName))
                                writer.WriteString(fieldName, field.Value);
                        }

                    // Write any state objects that weren't structured as fieldsets into an arbitrary array.
                    if (needToWriteState || needToWriteAnyScopeStates)
                    {
                        writer.WritePropertyName(propertyNamingPolicy.ConvertName("States"));
                        writer.WriteStartArray();

                        if (needToWriteState)
                            value.WriteStateAsValue(writer, options);

                        if (needToWriteAnyScopeStates)
                            foreach (var scopeState in value.ScopeStatesBuffer)
                                if (!writer.TryWriteFieldset(scopeState, options, usedFieldNames))
                                    JsonSerializer.Serialize(writer, scopeState, options);

                        writer.WriteEndArray();
                    }
                }
                finally
                {
                    _usedFieldNamesPool.Return(usedFieldNames);
                }
            }

            writer.WriteEndObject();
        }

        private readonly IOptions<SeqLoggerOptions>     _options;
        private readonly ObjectPool<HashSet<string>>    _usedFieldNamesPool;

        private class UsedFieldNamesPooledObjectPolicy
            : IPooledObjectPolicy<HashSet<string>>
        {
            public HashSet<string> Create()
                => new();

            public bool Return(HashSet<string> obj)
            {
                obj.Clear();

                return true;
            }
        }
    }
}
