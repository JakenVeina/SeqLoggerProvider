using System.Reflection;

namespace System.Text.Json.Serialization
{
    class MemberInfoWriteOnlyJsonConverter<T>
            : JsonConverter<T>
        where T : MemberInfo
    {
        public override bool CanConvert(Type typeToConvert)
            => typeof(T).IsAssignableFrom(typeToConvert);

        public override T? Read(
                ref Utf8JsonReader      reader,
                Type                    typeToConvert,
                JsonSerializerOptions   options)
            => throw new NotSupportedException();

        public override void Write(
                Utf8JsonWriter          writer,
                T                       value,
                JsonSerializerOptions   options)
            => writer.WriteStringValue((value is Type type)
                ? type.FullName
                : $"{value.DeclaringType}.{value.Name}");
    }
}
