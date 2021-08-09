using System.Collections.Generic;
using System.Text.Json;

using SeqLoggerProvider.Json;

namespace SeqLoggerProvider.Internal.Json
{
    internal static class Utf8JsonWriterExtensions
    {
        public static bool TryWriteFieldset<T>(
            this Utf8JsonWriter     writer,
            T                       value,
            JsonSerializerOptions   options,
            HashSet<string>         usedFieldNames)
        {
            if (value is not IReadOnlyList<KeyValuePair<string, object?>> fieldset)
                return false;

            var propertyNamingPolicy = options.PropertyNamingPolicy ?? PassthroughJsonNamingPolicy.Default;

            foreach (var field in fieldset)
            {
                if (field.Key == "{OriginalFormat}")
                    continue;

                var fieldName = propertyNamingPolicy.ConvertName(field.Key);
                if (!usedFieldNames.Add(fieldName))
                    continue;

                writer.WritePropertyName(fieldName);

                if (field.Value is null)
                    writer.WriteNullValue();
                else
                    JsonSerializer.Serialize(writer, field.Value, options);
            }

            return true;
        }
    }
}
