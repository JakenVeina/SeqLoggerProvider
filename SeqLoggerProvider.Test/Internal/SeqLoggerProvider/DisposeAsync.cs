using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NUnit.Framework;
using Shouldly;

using SeqLoggerProvider.Internal;
using SeqLoggerProvider.Utilities;

using Uut = SeqLoggerProvider.Internal.SeqLoggerProvider;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerProvider
{
    [TestFixture]
    public class DisposeAsync
    {
        [Test]
        public async Task ManagerIsRunning_WaitsForManagerToStop()
        {
            using var seqLoggerManager = new FakeSeqLoggerManager();

            using var serviceProvider = new ServiceCollection()
                .AddSingleton<ISeqLoggerManager>(seqLoggerManager)
                .BuildServiceProvider();

            var uut = new Uut(
                seqLoggerEventChannel:  new FakeSeqLoggerEventChannel(),
                serviceProvider:        serviceProvider,
                systemClock:            new FakeSystemClock());

            uut.SetScopeProvider(new FakeExternalScopeProvider());

            var logger = uut.CreateLogger("CategoryName");

            // Trigger the manager to run.
            logger.Log(LogLevel.Debug, "This is a test.");

            var result = uut.DisposeAsync();

            // Cancellation happens in the background, so give it time to trigger, but also don't let the test deadlock
            try
            {
                await Task.WhenAny(
                    seqLoggerManager.WhenStopRequested,
                    Task.Delay(TimeSpan.FromSeconds(5)));
            }
            catch (OperationCanceledException) { }

            seqLoggerManager.IsStopRequested.ShouldBeTrue();
            result.IsCompleted.ShouldBeFalse();

            seqLoggerManager.Stop();

            await result;
        }

        [Test]
        public async Task ManagerIsNotRunning_Succeeds()
        {
            using var seqLoggerManager = new FakeSeqLoggerManager();

            using var serviceProvider = new ServiceCollection()
                .AddSingleton<ISeqLoggerManager>(seqLoggerManager)
                .BuildServiceProvider();

            var uut = new Uut(
                seqLoggerEventChannel:  new FakeSeqLoggerEventChannel(),
                serviceProvider:        serviceProvider,
                systemClock:            new FakeSystemClock());

            uut.SetScopeProvider(new FakeExternalScopeProvider());

            var result = uut.DisposeAsync();

            result.IsCompletedSuccessfully.ShouldBeTrue();

            await result;
        }
    }
}
