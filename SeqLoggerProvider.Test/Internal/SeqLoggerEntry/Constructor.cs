using System.IO;

using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLoggerEntry;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerEntry
{
    [TestFixture]
    public class Constructor
    {
        [Test]
        public void Always_EntryIsEmpty()
        {
            using var uut = new Uut();

            uut.BufferLength    .ShouldBe(0);
            uut.CategoryName    .ShouldBe(string.Empty);
            uut.EventId.Id      .ShouldBe(default);
            uut.EventId.Name    .ShouldBe(string.Empty);
            uut.LogLevel        .ShouldBe(default);
            uut.OccurredUtc     .ShouldBe(default);

            using var buffer = new MemoryStream();

            uut.CopyBufferTo(buffer);

            buffer.Length.ShouldBe(0);
        }
    }
}
