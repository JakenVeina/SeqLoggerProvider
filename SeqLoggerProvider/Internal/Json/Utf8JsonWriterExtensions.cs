using System.Collections.Generic;
using System.Text.Json;

using SeqLoggerProvider.Json;

namespace SeqLoggerProvider.Internal.Json
{
    internal static class Utf8JsonWriterExtensions
    {
        public static bool TryWriteStateFields<TState>(
            this Utf8JsonWriter     writer,
            TState                  state,
            JsonSerializerOptions   options)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> fields)
                return false;

            var propertyNamingPolicy = options.PropertyNamingPolicy ?? PassthroughJsonNamingPolicy.Default;

            foreach (var field in fields)
            {
                if (field.Key == "{OriginalFormat}")
                    continue;

                writer.WritePropertyName(propertyNamingPolicy.ConvertName(field.Key));

                if (field.Value is null)
                    writer.WriteNullValue();
                else
                    JsonSerializer.Serialize(writer, field.Value, options);
            }
            return true;
        }
    }
}
