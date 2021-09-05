using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using NUnit.Framework;
using Shouldly;

namespace SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter
{
    [TestFixture]
    public class Write
    {
        private class TestType
        {
            public TestType(string value)
                => _value = value;

            public string Value
            {
                get => _value;
                set => _value = value;
            }

            public void MyMethod() { }

            public string _value;
        }

        public static readonly IReadOnlyList<TestCaseData> Always_TestCaseData
            = new[]
            {
                /*                  value,                                                  expectedResult  */
                new TestCaseData(   typeof(Write),                                          "\"SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter.Write\""                   ).SetName("{m}(Basic class)"),
                new TestCaseData(   typeof(TestType),                                       "\"SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter.Write+TestType\""          ).SetName("{m}(Nested class)"),
                new TestCaseData(   typeof(TestType).GetConstructors()[0],                  "\"SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter.Write+TestType..ctor\""    ).SetName("{m}(Constructor)"),
                new TestCaseData(   typeof(TestType).GetProperties()[0],                    "\"SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter.Write+TestType.Value\""    ).SetName("{m}(Property)"),
                new TestCaseData(   typeof(TestType).GetMethod(nameof(TestType.MyMethod)),  "\"SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter.Write+TestType.MyMethod\"" ).SetName("{m}(Method)"),
                new TestCaseData(   typeof(TestType).GetFields()[0],                        "\"SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter.Write+TestType._value\""   ).SetName("{m}(Field)")
            };

        [TestCaseSource(nameof(Always_TestCaseData))]
        public void Always_WritesExpectedName<T>(
                T       value,
                string  expectedResult)
            where T : MemberInfo
        {
            var uut = new global::System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter<T>();

            using var writerBuffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(writerBuffer, new JsonWriterOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
                uut.Write(writer, value, new());

            Encoding.UTF8.GetString(writerBuffer.ToArray()).ShouldBe(expectedResult);
        }
    }
}
