using NUnit.Framework;
using Shouldly;

using System;

using Uut = SeqLoggerProvider.Utilities.DefaultSystemClock;

namespace SeqLoggerProvider.Test.Utilities.DefaultSystemClock
{
    [TestFixture]
    public class UtcNow
    {
        [Test]
        public void Always_ResultIsUnique()
        {
            var uut = new Uut();

            uut.Now.ShouldNotBe(uut.Now);
        }

        [Test]
        public void Always_ResultIsCorrect()
        {
            var uut = new Uut();

            var from = DateTimeOffset.Now;

            var result = uut.Now;

            var to = DateTimeOffset.Now;

            result.ShouldBeInRange(from, to);
            result.Offset.ShouldBe(from.Offset);
        }
    }
}
