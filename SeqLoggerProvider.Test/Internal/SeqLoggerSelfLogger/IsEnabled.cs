using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Moq;
using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLoggerSelfLogger;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerSelfLogger
{
    [TestFixture]
    public class IsEnabled
    {
        [Test]
        public void LoggerHasNotMaterialized_ThrowsException()
        {
            var logger = Mock.Of<ILogger>();

            var uut = new Uut(() => logger);

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = uut.IsEnabled(default);
            });
        }

        public static readonly IReadOnlyList<TestCaseData> LoggerHasMaterialized_TestCaseData
            = new[]
            {
                /*                  logLevel,               expectedResult  */
                new TestCaseData(   LogLevel.Trace,         true            ).SetName("{m}(Trace)"),
                new TestCaseData(   LogLevel.Debug,         true            ).SetName("{m}(Debug)"),
                new TestCaseData(   LogLevel.Information,   true            ).SetName("{m}(Information)"),
                new TestCaseData(   LogLevel.Warning,       true            ).SetName("{m}(Warning)"),
                new TestCaseData(   LogLevel.Error,         true            ).SetName("{m}(Error)"),
                new TestCaseData(   LogLevel.Critical,      true            ).SetName("{m}(Critical)"),
                new TestCaseData(   LogLevel.None,          true            ).SetName("{m}(None)")
            };

        [TestCaseSource(nameof(LoggerHasMaterialized_TestCaseData))]
        public void LoggerHasMaterialized_InvokesLoggerIsEnabled(
            LogLevel    logLevel,
            bool        expectedResult)
        {
            var mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(expectedResult);

            var uut = new Uut(() => mockLogger.Object);

            uut.EnsureMaterialized();

            var result = uut.IsEnabled(logLevel);

            mockLogger.Verify(
                x => x.IsEnabled(logLevel),
                Times.Once);

            result.ShouldBe(expectedResult);
        }
    }
}
