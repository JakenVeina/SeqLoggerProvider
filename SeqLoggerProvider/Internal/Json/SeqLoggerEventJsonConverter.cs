using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using SeqLoggerProvider.Json;

namespace SeqLoggerProvider.Internal.Json
{
    internal class SeqLoggerEventJsonConverter
        : JsonConverter<SeqLoggerEvent>
    {
        public SeqLoggerEventJsonConverter(IOptions<SeqLoggerConfiguration> seqLoggerConfiguration)
            => _seqLoggerConfiguration = seqLoggerConfiguration;

        public override SeqLoggerEvent Read(
                ref Utf8JsonReader      reader,
                Type                    typeToConvert,
                JsonSerializerOptions   options)
            => throw new NotSupportedException();

        public override void Write(
            Utf8JsonWriter          writer,
            SeqLoggerEvent          value,
            JsonSerializerOptions   options)
        {
            var propertyNamingPolicy = options.PropertyNamingPolicy ?? PassthroughJsonNamingPolicy.Default;

            writer.WriteStartObject();

            writer.WriteString("@t", value.Occurred.ToString("o", CultureInfo.InvariantCulture));
            
            writer.WriteString("@l", value.LogLevel.ToString());

            if ((value.EventId.Id is not 0) || !string.IsNullOrWhiteSpace(value.EventId.Name))
            {
                writer.WriteNumber("@i", (uint)HashCode.Combine(value.EventId.Id, value.EventId.Name));

                writer.WritePropertyName(propertyNamingPolicy.ConvertName("EventId"));
                JsonSerializer.Serialize(writer, value.EventId, options);
            }

            if (!string.IsNullOrWhiteSpace(value.CategoryName))
                writer.WriteString(propertyNamingPolicy.ConvertName("CategoryName"), value.CategoryName);

            var message = value.BuildMessage();
            if (!string.IsNullOrWhiteSpace(message))
                writer.WriteString("@m", message);

            if (value.Exception is not null)
            {
                writer.WritePropertyName("@x");
                JsonSerializer.Serialize(writer, value.Exception, options);
            }

            var globalFields = _seqLoggerConfiguration.Value.GlobalScopeState;
            if (globalFields is not null)
                foreach (var field in globalFields)
                    writer.WriteString(propertyNamingPolicy.ConvertName(field.Key), field.Value);

            value.WriteState(writer, options);

            foreach(var scopeState in value.ScopeStates)
            {
                if ((scopeState is not null) && !writer.TryWriteStateFields(scopeState, options))
                {
                    writer.WritePropertyName(propertyNamingPolicy.ConvertName("ScopeStates"));
                    writer.WriteStartArray();
                    JsonSerializer.Serialize(writer, scopeState, options);
                    writer.WriteEndArray();
                }
            }

            writer.WriteEndObject();
        }

        private readonly IOptions<SeqLoggerConfiguration> _seqLoggerConfiguration;
    }
}
