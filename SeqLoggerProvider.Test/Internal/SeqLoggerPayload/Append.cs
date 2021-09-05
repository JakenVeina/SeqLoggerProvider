using System.IO;

using Moq;
using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLoggerPayload;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayload
{
    [TestFixture]
    public class Append
    {
        [Test]
        public void PayloadIsEmpty_AppendsEventBuffer()
        {
            using var uut = new Uut();

            var mockSeqLoggerEntry = new Mock<ISeqLoggerEntry>();

            uut.Append(mockSeqLoggerEntry.Object);

            uut.EntryCount.ShouldBe(1);

            mockSeqLoggerEntry.Verify(
                x => x.CopyBufferTo(It.IsAny<Stream>()),
                Times.Once);

            mockSeqLoggerEntry.Verify(
                x => x.CopyBufferTo(uut.Buffer),
                Times.Once);

            uut.Buffer.Length.ShouldBe(0);
        }

        [Test]
        public void PayloadIsNotEmpty_AppendsSeparatorAndEventBuffer()
        {
            using var uut = new Uut();

            var mockSeqLoggerEntry = new Mock<ISeqLoggerEntry>();

            uut.Append(mockSeqLoggerEntry.Object);

            mockSeqLoggerEntry.Invocations.Clear();

            uut.Append(mockSeqLoggerEntry.Object);
            
            uut.EntryCount.ShouldBe(2);

            mockSeqLoggerEntry.Verify(
                x => x.CopyBufferTo(It.IsAny<Stream>()),
                Times.Once);

            mockSeqLoggerEntry.Verify(
                x => x.CopyBufferTo(uut.Buffer),
                Times.Once);

            uut.Buffer.Length.ShouldBe(1);

            uut.Buffer.ShouldBeOfType<MemoryStream>().GetBuffer()[0].ShouldBe((byte)'\n');
        }
    }
}
