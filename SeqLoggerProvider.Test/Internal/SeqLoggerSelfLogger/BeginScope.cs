using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.Logging;

using Moq;
using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLoggerSelfLogger;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerSelfLogger
{
    [TestFixture]
    public class BeginScope
    {
        [Test]
        public void LoggerHasNotMaterialized_ThrowsException()
        {
            var logger = Mock.Of<ILogger>();

            var uut = new Uut(() => logger);

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = uut.BeginScope(default(object?));
            });
        }

        public static readonly IReadOnlyList<TestCaseData> LoggerHasMaterialized_TestCaseData
            = new[]
            {
                /*                  state           */
                new TestCaseData(   null            ).SetName("{m}(Null state)"),
                new TestCaseData(   new object()    ).SetName("{m}(Object state)"),
                new TestCaseData(   "String state"  ).SetName("{m}(String state)"),
                new TestCaseData(   int.MaxValue    ).SetName("{m}(Number state)")
            };

        [TestCaseSource(nameof(LoggerHasMaterialized_TestCaseData))]
        public void LoggerHasMaterialized_InvokesLoggerBeginScope(object? state)
            => GetType()
                .GetMethod(nameof(LoggerHasMaterialized_InvokesLoggerBeginScope), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(state?.GetType() ?? typeof(object))
                .Invoke(null, new[] { state });
        
        private static void LoggerHasMaterialized_InvokesLoggerBeginScope<TState>(TState state)
        {
            var mockLogger = new Mock<ILogger>();
            var disposal = Mock.Of<IDisposable>();
            mockLogger
                .Setup(x => x.BeginScope(It.IsAny<TState>()))
                .Returns(disposal);

            var uut = new Uut(() => mockLogger.Object);

            uut.EnsureMaterialized();

            var result = uut.BeginScope(state);

            mockLogger.Verify(
                x => x.BeginScope(state),
                Times.Once);

            result.ShouldBeSameAs(disposal);
        }
    }
}
