using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;
using Shouldly;

using Uut = System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverterFactory;

namespace SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverterFactory
{
    [TestFixture]
    public class CanConvert
    {
        public static readonly IReadOnlyList<TestCaseData> Always_TestCaseData
            = new[]
            {
                /*                  typeToConvert,              expectedResult  */
                new TestCaseData(   typeof(MemberInfo),         true            ).SetName("{m}(Can convert MemberInfo)"),
                new TestCaseData(   typeof(Type),               true            ).SetName("{m}(Can convert Type)"),
                new TestCaseData(   typeof(TypeInfo),           true            ).SetName("{m}(Can convert TypeInfo)"),
                new TestCaseData(   typeof(FieldInfo),          true            ).SetName("{m}(Can convert FieldInfo)"),
                new TestCaseData(   typeof(PropertyInfo),       true            ).SetName("{m}(Can convert PropertyInfo)"),
                new TestCaseData(   typeof(MethodInfo),         true            ).SetName("{m}(Can convert MethodInfo)"),
                new TestCaseData(   typeof(ConstructorInfo),    true            ).SetName("{m}(Can convert ConstructorInfo)"),
                new TestCaseData(   typeof(Assembly),           false           ).SetName("{m}(Cannot convert Assembly)"),
                new TestCaseData(   typeof(Module),             false           ).SetName("{m}(Cannot convert Module)"),
                new TestCaseData(   typeof(object),             false           ).SetName("{m}(Cannot convert object)")
            };

        [TestCaseSource(nameof(Always_TestCaseData))]
        public void Always_ResultIsExpected(
            Type typeToConvert,
            bool expectedResult)
        {
            var uut = new Uut();

            uut.CanConvert(typeToConvert).ShouldBe(expectedResult);
        }
    }
}
