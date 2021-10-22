namespace System.Text.Json.Serialization
{
    internal sealed class ValueToStringWriteOnlyJsonConverter<T>
            : WriteOnlyJsonConverter<T>
        where T : struct
    {
        public override void Write(
                Utf8JsonWriter          writer,
                T                       value,
                JsonSerializerOptions   options)
            => writer.WriteStringValue(value.ToString());
    }
}
