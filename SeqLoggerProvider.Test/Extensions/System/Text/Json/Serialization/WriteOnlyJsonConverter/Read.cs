using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

using Moq;
using NUnit.Framework;
using Shouldly;

namespace SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.WriteOnlyJsonConverter
{
    [TestFixture]
    public class Read
    {
        public static readonly IReadOnlyList<TestCaseData> Always_TestCaseData
            = new[]
            {
                new TestCaseData(typeof(int)    ).SetName("{m}(Int32)"),
                new TestCaseData(typeof(string) ).SetName("{m}(String)"),
                new TestCaseData(typeof(object) ).SetName("{m}(Object)"),
            };

        [TestCaseSource(nameof(Always_TestCaseData))]
        public void Always_ThrowsException(Type t)
            => GetType()
                .GetMethod(nameof(Always_ThrowsException), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(t)
                .Invoke(null, null);

        private static void Always_ThrowsException<T>()
        {
            var uut = Mock.Of<global::System.Text.Json.Serialization.WriteOnlyJsonConverter<T>>();

            Should.Throw<NotSupportedException>(() =>
            {
                Utf8JsonReader reader = default;

                uut.Read(ref reader, typeof(T), new());
            });
        }
    }
}
