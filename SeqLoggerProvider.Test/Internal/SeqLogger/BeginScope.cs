using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using NUnit.Framework;
using Shouldly;

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
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            var result = uut.BeginScope(state);

            testContext.ScopeProvider.ActiveStatesByDisposal.ShouldHaveSingleItem().Key.ShouldBe(result);
            testContext.ScopeProvider.ActiveStatesByDisposal.ShouldHaveSingleItem().Value.ShouldBe(state);
        }
    }
}
