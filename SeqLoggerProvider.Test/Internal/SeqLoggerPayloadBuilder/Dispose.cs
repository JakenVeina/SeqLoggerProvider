using NUnit.Framework;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayloadBuilder
{
    [TestFixture]
    public class Dispose
    {
        [Test]
        public void Always_AllowsMultipleDisposal()
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            uut.Dispose();
            uut.Dispose();
        }
    }
}
