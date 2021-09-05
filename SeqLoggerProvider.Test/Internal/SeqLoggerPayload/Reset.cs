using Moq;
using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLoggerPayload;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayload
{
    [TestFixture]
    public class Reset
    {
        [Test]
        public void Always_ClearsBufferAndEntryCount()
        {
            using var uut = new Uut();

            uut.Append(Mock.Of<ISeqLoggerEntry>());
            uut.Buffer.WriteByte(1);

            uut.Reset();

            uut.Buffer.Length.ShouldBe(0);
            uut.EntryCount.ShouldBe(0);
        }
    }
}
