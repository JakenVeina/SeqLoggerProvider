namespace System.Text.Json.Serialization
{
    public class FakeJsonConverter<T>
        : JsonConverter<T>
    {
        public FakeJsonConverter(Action<Utf8JsonWriter, T, JsonSerializerOptions> onWrite)
            => _onWrite = onWrite;

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => _onWrite(writer, value, options);

        private readonly Action<Utf8JsonWriter, T, JsonSerializerOptions> _onWrite;
    }
}
