using Moq;
using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLoggerEntryPooledObjectPolicy;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerEntryPooledObjectPolicy
{
    [TestFixture]
    public class Return
    {
        [Test]
        public void Always_ResetsAndReusesEvent()
        {
            var uut = new Uut();

            var mockObj = new Mock<ISeqLoggerEntry>();

            var result = uut.Return(mockObj.Object);

            result.ShouldBe(true);

            mockObj.Verify(
                x => x.Reset(),
                Times.Once);
        }
    }
}
