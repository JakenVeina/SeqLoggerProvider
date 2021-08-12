using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;
using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;
using SeqLoggerProvider.Utilities;

using Uut = SeqLoggerProvider.SeqLoggerProvider;

namespace SeqLoggerProvider.Test.SeqLoggerProvider
{
    [TestFixture]
    public class CreateLogger
    {
        [Test]
        public async Task ExternalScopeProviderHasNotBeenInitialized_ThrowsException()
        {
            using var serviceProvider = new ServiceCollection()
                .AddSingleton<ISeqLoggerEventChannel>(new FakeSeqLoggerEventChannel())
                .AddSingleton<ISystemClock>(new FakeSystemClock())
                .BuildServiceProvider();

            var uut = new Uut(
                onDisposedAsync:    Mock.Of<Func<ValueTask>>(),
                serviceProvider:    serviceProvider);
            
            Should.Throw<InvalidOperationException>(() =>
            {
                _ = uut.CreateLogger("CategoryName");
            });

            await uut.DisposeAsync();
        }

        [TestCase("")]
        [TestCase("CategoryName")]
        [TestCase("This is a test")]
        public async Task Otherwise_CreatesLoggerAndRunsManagerOnFirstLog(string categoryName)
        {
            using var seqLoggerManager = new FakeSeqLoggerManager();

            using var serviceProvider = new ServiceCollection()
                .AddSingleton<ISeqLoggerEventChannel>(new FakeSeqLoggerEventChannel())
                .AddSingleton<ISeqLoggerManager>(seqLoggerManager)
                .AddSingleton<ISystemClock>(new FakeSystemClock())
                .BuildServiceProvider();

            var uut = new Uut(
                onDisposedAsync:    Mock.Of<Func<ValueTask>>(),
                serviceProvider:    serviceProvider);

            uut.SetScopeProvider(new FakeExternalScopeProvider());

            var result = uut.CreateLogger(categoryName);

            var logger = result.ShouldBeOfType<SeqLogger>();

            logger.CategoryName.ShouldBe(categoryName);

            seqLoggerManager.IsRunning.ShouldBeFalse();

            // Trigger the manager to run.
            logger.Log(LogLevel.Debug, "This is a test.");

            // The manager runs in the background, so give it time to start, but also don't let the test deadlock
            try
            {
                await Task.WhenAny(
                    seqLoggerManager.WhenStarted,
                    Task.Delay(TimeSpan.FromSeconds(5)));
            }
            catch (OperationCanceledException) { }

            seqLoggerManager.IsRunning.ShouldBeTrue();

            // Make sure the manager is only run once.
            logger.Log(LogLevel.Debug, "This is a test.");

            seqLoggerManager.IsRunning.ShouldBeTrue();
        }
    }
}
