using System.Reflection;

namespace System.Text.Json.Serialization
{
    internal sealed class MemberInfoWriteOnlyJsonConverterFactory
        : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
            => typeof(MemberInfo).IsAssignableFrom(typeToConvert);

        public override JsonConverter? CreateConverter(
                Type                    typeToConvert,
                JsonSerializerOptions   options)
            => (JsonConverter)Activator.CreateInstance(typeof(MemberInfoWriteOnlyJsonConverter<>)
                .MakeGenericType(typeToConvert));
    }
}
