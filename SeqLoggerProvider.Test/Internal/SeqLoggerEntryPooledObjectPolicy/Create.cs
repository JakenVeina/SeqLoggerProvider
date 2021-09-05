using System.IO;

using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLoggerEntryPooledObjectPolicy;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerEntryPooledObjectPolicy
{
    [TestFixture]
    public class Create
    {
        [Test]
        public void Always_CreatesEmptyEntry()
        {
            var uut = new Uut();

            var result = uut.Create().ShouldBeOfType<global::SeqLoggerProvider.Internal.SeqLoggerEntry>();

            result.BufferLength .ShouldBe(0);
            result.CategoryName .ShouldBe(string.Empty);
            result.EventId.Id   .ShouldBe(default);
            result.EventId.Name .ShouldBe(string.Empty);
            result.LogLevel     .ShouldBe(default);
            result.OccurredUtc  .ShouldBe(default);

            using var buffer = new MemoryStream();

            result.CopyBufferTo(buffer);

            buffer.Length.ShouldBe(0);
        }
    }
}
