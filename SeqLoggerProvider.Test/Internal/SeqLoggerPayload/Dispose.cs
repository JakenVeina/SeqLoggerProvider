using NUnit.Framework;

using Uut = SeqLoggerProvider.Internal.SeqLoggerPayload;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayload
{
    [TestFixture]
    public class Dispose
    {
        [Test]
        public void DisposeHasBeenInvoked_DoesNothing()
        {
            using var uut = new Uut();

            uut.Dispose();
        }
    }
}
