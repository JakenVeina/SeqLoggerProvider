using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using NUnit.Framework;
using Shouldly;

namespace SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.ValueToStringWriteOnlyJsonConverter
{
    [TestFixture]
    public class Write
    {
        public static readonly IReadOnlyList<TestCaseData> Always_TestCaseData
            = new[]
            {
                /*                  value,              */
                new TestCaseData(   long.MinValue       ).SetName("{m}(Int64.MinValue)"),
                new TestCaseData(   long.MaxValue       ).SetName("{m}(Int64.MaxValue)"),
                new TestCaseData(   ulong.MinValue      ).SetName("{m}(UInt64.MinValue)"),
                new TestCaseData(   ulong.MaxValue      ).SetName("{m}(UInt64.MaxValue)"),
                new TestCaseData(   decimal.MinValue    ).SetName("{m}(Decimal.MinValue)"),
                new TestCaseData(   decimal.MaxValue    ).SetName("{m}(Decimal.MaxValue)")
            };

        [TestCaseSource(nameof(Always_TestCaseData))]
        public void Always_ValueAsString<T>(T value)
            where T : struct
        {
            var uut = new global::System.Text.Json.Serialization.ValueToStringWriteOnlyJsonConverter<T>();

            using var writerBuffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(writerBuffer, new JsonWriterOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
                uut.Write(writer, value, new());

            Encoding.UTF8.GetString(writerBuffer.ToArray()).ShouldBe($"\"{value}\"");
        }
    }
}
