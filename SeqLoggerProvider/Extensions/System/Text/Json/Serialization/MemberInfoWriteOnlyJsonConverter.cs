using System.Reflection;

namespace System.Text.Json.Serialization
{
    internal sealed class MemberInfoWriteOnlyJsonConverter<T>
            : WriteOnlyJsonConverter<T>
        where T : MemberInfo
    {
        public override bool CanConvert(Type typeToConvert)
            => typeof(T).IsAssignableFrom(typeToConvert);

        public override void Write(
                Utf8JsonWriter          writer,
                T                       value,
                JsonSerializerOptions   options)
            => writer.WriteStringValue((value is Type type)
                ? type.FullName
                : $"{value.DeclaringType}.{value.Name}");
    }
}
