using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;
using SeqLoggerProvider.Utilities;

using Uut = SeqLoggerProvider.Internal.SeqLogger;

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
        public void Always_ReturnsTrue(LogLevel logLevel)
        {
            var uut = new Uut(
                categoryName:           "CategoryName",
                externalScopeProvider:  new FakeExternalScopeProvider(),
                onLog:                  () => { },
                seqLoggerEventChannel:  new FakeSeqLoggerEventChannel(),
                systemClock:            new FakeSystemClock());

            var result = uut.IsEnabled(logLevel);

            result.ShouldBeTrue();
        }
    }
}
