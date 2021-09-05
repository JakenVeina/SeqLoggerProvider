using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;
using Shouldly;

using Uut = System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverterFactory;

namespace SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverterFactory
{
    [TestFixture]
    public class CreateConverter
    {
        public static readonly IReadOnlyList<TestCaseData> Always_TestCaseData
            = new[]
            {
                new TestCaseData(typeof(MemberInfo)         ).SetName("{m}(MemberInfo)"),
                new TestCaseData(typeof(Type)               ).SetName("{m}(Type)"),
                new TestCaseData(typeof(TypeInfo)           ).SetName("{m}(TypeInfo)"),
                new TestCaseData(typeof(FieldInfo)          ).SetName("{m}(FieldInfo)"),
                new TestCaseData(typeof(ConstructorInfo)    ).SetName("{m}(ConstructorInfo)"),
                new TestCaseData(typeof(PropertyInfo)       ).SetName("{m}(PropertyInfo)"),
                new TestCaseData(typeof(MethodInfo)         ).SetName("{m}(MethodInfo)")
            };

        [TestCaseSource(nameof(Always_TestCaseData))]
        public void Always_CreatesConverter(Type typeToConvert)
            => GetType()
                .GetMethod(nameof(Always_CreatesConverter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeToConvert)
                .Invoke(null, null);

        private static void Always_CreatesConverter<TTypeToConvert>()
            where TTypeToConvert : MemberInfo
        {
            var uut = new Uut();

            var result = uut.CreateConverter(typeof(TTypeToConvert), new());

            result.ShouldBeOfType<global::System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter<TTypeToConvert>>();
        }
    }
}
