using System;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLoggerEntry;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerEntry
{
    [TestFixture]
    public class Reset
    {
        [Test]
        public void Always_ResetsState()
        {
            using var uut = new Uut();

            uut.Load(
                categoryName:   "CategoryName",
                eventId:        new(1, "EventName"),
                exception:      null,
                formatter:      (_, _) => "This is a test event.",
                globalFields:   null,
                logLevel:       LogLevel.Debug,
                occurredUtc:    DateTime.UnixEpoch,
                scopeProvider:  new FakeExternalScopeProvider(),
                state:          default(object?),
                options:        new JsonSerializerOptions());

            uut.Reset();

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
