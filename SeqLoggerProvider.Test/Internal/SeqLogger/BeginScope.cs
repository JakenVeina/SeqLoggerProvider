using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;
using SeqLoggerProvider.Utilities;

using Uut = SeqLoggerProvider.Internal.SeqLogger;

namespace SeqLoggerProvider.Test.Internal.SeqLogger
{
    [TestFixture]
    public class BeginScope
    {
        public static IReadOnlyList<TestCaseData> Always_TestCaseData
            => new[]
            {
                new TestCaseData(null),
                new TestCaseData(1),
                new TestCaseData("StateString"),
                new TestCaseData(new object())
            };

        [TestCaseSource(nameof(Always_TestCaseData))]
        public void Always_PushesState(object? state)
        {
            Expression<Action> expression = () => Always_PushesState(string.Empty);
            ((MethodCallExpression)expression.Body)
                .Method
                .GetGenericMethodDefinition()
                .MakeGenericMethod(state?.GetType() ?? typeof(object))
                .Invoke(null, new[] { state });
        }

        private static void Always_PushesState<T>(T state)
        {
            var externalScopeProvider = new FakeExternalScopeProvider();

            var uut = new Uut(
                categoryName:           "CategoryName",
                externalScopeProvider:  externalScopeProvider,
                onLog:                  () => { },
                seqLoggerEventChannel:  new FakeSeqLoggerEventChannel(),
                systemClock:            new FakeSystemClock());

            var result = uut.BeginScope(state);

            externalScopeProvider.StatesByDisposal.Values.First().ShouldBe(state);

            result.ShouldBeSameAs(externalScopeProvider.StatesByDisposal.Keys.First());
        }
    }
}
