using NUnit.Framework;

using Uut = SeqLoggerProvider.Internal.SeqLoggerEntry;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerEntry
{
    [TestFixture]
    public class Dispose
    {
        [Test]
        public void HasBeenDisposed_DoesNothing()
        {
            using var uut = new Uut();

            uut.Dispose();
        }
    }
}
