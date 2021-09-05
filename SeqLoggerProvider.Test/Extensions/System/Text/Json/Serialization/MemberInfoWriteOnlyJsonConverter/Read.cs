﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

using NUnit.Framework;
using Shouldly;

namespace SeqLoggerProvider.Test.Extensions.System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter
{
    [TestFixture]
    public class Read
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
        public void Always_ThrowsException(Type t)
            => GetType()
                .GetMethod(nameof(Always_ThrowsException), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(t)
                .Invoke(null, null);

        private static void Always_ThrowsException<T>()
            where T : MemberInfo
        {
            var uut = new global::System.Text.Json.Serialization.MemberInfoWriteOnlyJsonConverter<Type>();

            Should.Throw<NotSupportedException>(() =>
            {
                Utf8JsonReader reader = default;

                uut.Read(ref reader, typeof(T), new());
            });
        }
    }
}
