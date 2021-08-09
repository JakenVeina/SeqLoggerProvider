using System;
using System.Text.Json;

using Microsoft.Extensions.Options;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

using Uut = SeqLoggerProvider.Internal.Json.SeqLoggerEventJsonConverter;

namespace SeqLoggerProvider.Test.Internal.Json.SeqLoggerEventJsonConverter
{
    [TestFixture]
    internal class Read
    {
        [Test]
        public void Always_ThrowsException()
        {
            var seqLoggerConfiguration = FakeOptions.Create(new SeqLoggerConfiguration());

            var uut = new Uut(seqLoggerConfiguration);

            var options = new JsonSerializerOptions();
            options.Converters.Add(uut);

            Should.Throw<NotSupportedException>(() =>
            {
                _ = JsonSerializer.Deserialize<SeqLoggerEvent>("{}", options);
            });
        }
    }
}
