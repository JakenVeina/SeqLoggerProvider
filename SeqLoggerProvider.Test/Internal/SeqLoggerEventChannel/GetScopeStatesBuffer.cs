using System.Collections.Generic;

using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLoggerEventChannel;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerEventChannel
{
    [TestFixture]
    public class GetScopeStatesBuffer
    {
        [Test]
        public void BufferIsNotAvailable_CreatesEmptyBuffer()
        {
            var uut = new Uut();

            var result1 = uut.GetScopeStatesBuffer();
            var result2 = uut.GetScopeStatesBuffer();

            result1.ShouldNotBeSameAs(result2);

            result1.ShouldBeEmpty();
            result2.ShouldBeEmpty();
        }

        [Test]
        public void BufferIsAvailable_ClearsAndReusesBuffer()
        {
            var uut = new Uut();

            var buffer = new List<object>()
            {
                new object()
            };

            uut.ReturnScopeStatesBuffer(buffer);

            var reusedBuffer = uut.GetScopeStatesBuffer();

            reusedBuffer.ShouldBeSameAs(buffer);
            reusedBuffer.ShouldBeEmpty();
        }
    }
}
