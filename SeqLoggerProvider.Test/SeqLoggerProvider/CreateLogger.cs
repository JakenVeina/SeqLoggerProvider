using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.SeqLoggerProvider
{
    [TestFixture]
    public class CreateLogger
    {
        [Test]
        public void ScopeProviderHasNotBeenSet_ThrowsException()
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = uut.CreateLogger("CategoryName");
            });
        }

        [TestCase("")]
        [TestCase("CategoryName")]
        public void ScopeProviderHasBeenSet_CreatesLoggerAndFinishesInitializationsOnCreatingLogEntry(string categoryName)
        {
            using var testContext = new TestContext();

            testContext.Options.Value.GlobalFields = new Dictionary<string, string>();
            testContext.SystemClock.Now = DateTimeOffset.UnixEpoch;

            var uut = testContext.BuildUut();

            var scopeProvider = new FakeExternalScopeProvider();

            uut.SetScopeProvider(scopeProvider);

            var result = uut.CreateLogger(categoryName).ShouldBeOfType<SeqLogger>();

            testContext.SelfLogger.IsMaterialized.ShouldBeFalse();
            testContext.Manager.HasStarted.ShouldBeFalse();

            result.Log(
                logLevel:   LogLevel.Debug,
                eventId:    new EventId(1, "Event"),
                state:      default(object?),
                exception:  null,
                formatter:  (_, _) => "");

            testContext.SelfLogger.IsMaterialized.ShouldBeTrue();
            testContext.Manager.HasStarted.ShouldBeTrue();

            var entry = testContext.EntryPool.CreatedObjects.ShouldHaveSingleItem().ShouldBeOfType<FakeSeqLoggerEntry>();

            testContext.EntryChannelWriter.Items.ShouldHaveSingleItem().ShouldBeSameAs(entry);

            var loadInvocation = entry.LoadInvocations.ShouldHaveSingleItem();
            loadInvocation.CategoryName.ShouldBe(categoryName);
            loadInvocation.GlobalFields.ShouldBeSameAs(testContext.Options.Value.GlobalFields);
            loadInvocation.OccurredUtc.ShouldBe(testContext.SystemClock.Now.UtcDateTime);
            loadInvocation.Options.ShouldBeSameAs(testContext.JsonSerializerOptions.Value);
            loadInvocation.ScopeProvider.ShouldBeSameAs(scopeProvider);
        }
    }
}
