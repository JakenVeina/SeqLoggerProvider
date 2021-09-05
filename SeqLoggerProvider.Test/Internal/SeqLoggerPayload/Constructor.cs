using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLoggerPayload;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayload
{
    [TestFixture]
    public class Constructor
    {
        [Test]
        public void Always_PayloadIsEmpty()
        {
            using var uut = new Uut();

            uut.Buffer.ShouldNotBeNull();
            uut.Buffer.Length.ShouldBe(0);

            uut.EntryCount.ShouldBe(0);
        }
    }
}
