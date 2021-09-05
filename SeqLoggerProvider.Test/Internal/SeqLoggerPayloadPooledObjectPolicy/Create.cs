using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLoggerPayloadPooledObjectPolicy;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerPayloadPooledObjectPolicy
{
    [TestFixture]
    public class Create
    {
        [Test]
        public void Always_CreatesEmptyPayload()
        {
            var uut = new Uut();

            var result = uut.Create().ShouldBeOfType<global::SeqLoggerProvider.Internal.SeqLoggerPayload>();

            result.Buffer.ShouldNotBeNull();
            result.Buffer.Length.ShouldBe(0);

            result.EntryCount.ShouldBe(0);
        }
    }
}
