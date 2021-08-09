using Microsoft.Extensions.Logging;

using NUnit.Framework;

using SeqLoggerProvider.Internal;
using SeqLoggerProvider.Utilities;

using Shouldly;

using Uut = SeqLoggerProvider.Internal.SeqLogger;

namespace SeqLoggerProvider.Test.Internal.SeqLogger
{
    [TestFixture]
    public class Constructor
    {
        [TestCase("")]
        [TestCase("CategoryName")]
        [TestCase("This is a test")]
        public void Always_InitializesCategoryName(string categoryName)
        {
            var uut = new Uut(
                categoryName:           categoryName,
                externalScopeProvider:  new FakeExternalScopeProvider(),
                onLog:                  () => { },
                seqLoggerEventChannel:  new FakeSeqLoggerEventChannel(),
                systemClock:            new FakeSystemClock());

            uut.CategoryName.ShouldBe(categoryName);
        }
    }
}
