using System.IO;
using System.Linq;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayloadBuilder
{
    [TestFixture]
    public class IsPayloadDataAvailable
    {
        [Test]
        public void ChannelHasEvents_ReturnsTrue()
        {
            using var testContext = new TestContext();

            testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create<object?>(
                state: default));

            var uut = testContext.BuildUut();

            uut.IsPayloadDataAvailable.ShouldBeTrue();
        }

        [Test]
        public void LastPayloadOverflowed_ReturnsTrue()
        {
            using var testContext = new TestContext();

            testContext.Configuration.Value = new()
            {
                MaxPayloadSize = 25
            };

            foreach(var i in Enumerable.Range(1, 10))
                testContext.EventChannel.WriteEvent(TestSeqLoggerEvent.Create(
                    state:      i,
                    message:    $"This is test event #{i}"));

            var uut = testContext.BuildUut();

            // Trigger a payload overflow
            using var payloadBuffer = new MemoryStream();
            _ = uut.AppendPayloadData(payloadBuffer);

            while (testContext.EventChannel.TryReadEvent() is not null);

            uut.IsPayloadDataAvailable.ShouldBeTrue();
        }

        [Test]
        public void Otherwise_ReturnsFalse()
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            uut.IsPayloadDataAvailable.ShouldBeFalse();
        }
    }
}
