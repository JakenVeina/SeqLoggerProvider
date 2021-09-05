using System.Threading;

using NUnit.Framework;
using Shouldly;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerManager
{
    [TestFixture]
    public class Constructor
    {
        [Test]
        public void Always_HasNotStartedOrStopped()
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            using var stopTokenSource = new CancellationTokenSource();

            uut.HasStarted.ShouldBeFalse();
            uut.WhenStopped.IsCompleted.ShouldBeFalse();
        }
    }
}
