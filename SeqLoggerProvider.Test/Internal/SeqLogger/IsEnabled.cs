using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

namespace SeqLoggerProvider.Test.Internal.SeqLogger
{
    [TestFixture]
    public class IsEnabled
    {
        [TestCase(LogLevel.Trace)]
        [TestCase(LogLevel.Debug)]
        [TestCase(LogLevel.Information)]
        [TestCase(LogLevel.Warning)]
        [TestCase(LogLevel.Error)]
        [TestCase(LogLevel.Critical)]
        public void LogLevelIsNotNone_ReturnsTrue(LogLevel logLevel)
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            var result = uut.IsEnabled(logLevel);

            result.ShouldBeTrue();
        }

        [Test]
        public void LogLevelIsNone_ReturnsFalse()
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            var result = uut.IsEnabled(LogLevel.None);

            result.ShouldBeFalse();
        }
    }
}
