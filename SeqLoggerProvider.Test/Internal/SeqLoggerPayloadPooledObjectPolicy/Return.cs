using Moq;
using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.SeqLoggerPayloadPooledObjectPolicy;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayloadPooledObjectPolicy
{
    [TestFixture]
    public class Return
    {
        [Test]
        public void Always_ResetsAndReusesEvent()
        {
            var uut = new Uut();

            var mockObj = new Mock<ISeqLoggerPayload>();

            var result = uut.Return(mockObj.Object);

            result.ShouldBe(true);

            mockObj.Verify(
                x => x.Reset(),
                Times.Once);
        }
    }
}
