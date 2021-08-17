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
            var seqLoggerOptions = FakeOptions.Create(new SeqLoggerOptions());

            var uut = new Uut(seqLoggerOptions);

            var options = new JsonSerializerOptions();
            options.Converters.Add(uut);

            Should.Throw<NotSupportedException>(() =>
            {
                _ = JsonSerializer.Deserialize<ISeqLoggerEvent>("{}", options);
            });
        }
    }
}
