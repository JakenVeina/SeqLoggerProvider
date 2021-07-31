using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider.Json
{
    internal class EventIdJsonConverter
        : JsonConverter<EventId>
    {
        public static readonly EventIdJsonConverter Default
            = new();

        public override EventId Read(
            ref Utf8JsonReader      reader,
            Type                    typeToConvert,
            JsonSerializerOptions   options)
        {
            if (reader.TokenType is not JsonTokenType.StartObject)
                throw new JsonException($"Expected StartObject token, encountered {reader.TokenType} instead.");

            var propertyNamingPolicy = options.PropertyNamingPolicy ?? PassthroughJsonNamingPolicy.Default;

            var idPropertyName      = propertyNamingPolicy.ConvertName("Id");
            var namePropertyName    = propertyNamingPolicy.ConvertName("Name");

            var id      = default(int);
            var name    = default(string?);

            var objectDepth = 0;
            while (reader.Read())
            {
                switch(reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        if (--objectDepth is 0)
                            return new EventId(id, name);
                        break;

                    case JsonTokenType.PropertyName:
                        var propertyName = reader.GetString();
                        if (!reader.Read())
                            throw new JsonException("The document is incomplete");

                        if (propertyName == idPropertyName)
                            id = reader.GetInt32();
                        else if (propertyName == namePropertyName)
                            name = reader.GetString();

                        break;

                    case JsonTokenType.StartObject:
                        ++objectDepth;
                        break;
                }
            }
            
            throw new JsonException("The document is incomplete");
        }

        public override void Write(
            Utf8JsonWriter          writer,
            EventId                 value,
            JsonSerializerOptions   options)
        {
            var propertyNamingPolicy = options.PropertyNamingPolicy ?? PassthroughJsonNamingPolicy.Default;

            writer.WriteStartObject();
            if (value.Id is not 0)
                writer.WriteNumber(propertyNamingPolicy.ConvertName("Id"), value.Id);
            if (!string.IsNullOrWhiteSpace(value.Name))
                writer.WriteString(propertyNamingPolicy.ConvertName("Name"), value.Name);
            writer.WriteEndObject();
        }
    }
}
