using NUnit.Framework;
using Shouldly;

using Uut = System.Text.Json.PassthroughJsonNamingPolicy;

namespace SeqLoggerProvider.Test.System.Text.Json.PassthroughJsonNamingPolicy
{
    [TestFixture]
    public class ConvertName
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("Test String")]
        public void Always_ResultIsName(string name)
        {
            var uut = Uut.Default;

            var result = uut.ConvertName(name);

            result.ShouldBe(name);
        }
    }
}
