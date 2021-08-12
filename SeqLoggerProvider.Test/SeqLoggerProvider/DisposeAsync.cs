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
    public class DisposeAsync
    {
        [Test]
        public async Task ManagerIsRunning_WaitsForManagerRunAsyncAndOnDisposedAsync()
        {
            using var seqLoggerManager = new FakeSeqLoggerManager();

            using var serviceProvider = new ServiceCollection()
                .AddSingleton<ISeqLoggerEventChannel>(new FakeSeqLoggerEventChannel())
                .AddSingleton<ISeqLoggerManager>(seqLoggerManager)
                .AddSingleton<ISystemClock>(new FakeSystemClock())
                .BuildServiceProvider();

            var onDisposedAsyncSource = new TaskCompletionSource();
            var mockOnDisposedAsync = new Mock<Func<ValueTask>>();
            mockOnDisposedAsync
                .Setup(x => x())
                .Returns(new ValueTask(onDisposedAsyncSource.Task));

            var uut = new Uut(
                onDisposedAsync:    mockOnDisposedAsync.Object,
                serviceProvider:    serviceProvider);

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

            // Verify that the manager received the stop request, and that we're waiting for it to stop.
            seqLoggerManager.IsStopRequested.ShouldBeTrue();
            result.IsCompleted.ShouldBeFalse();
            mockOnDisposedAsync.Verify(
                x => x(),
                Times.Never);

            seqLoggerManager.Stop();
            await Task.Yield();

            // Verify that we're now waiting for onDisposedAsync().
            mockOnDisposedAsync.Verify(
                x => x(),
                Times.Once);
            result.IsCompleted.ShouldBeFalse();

            onDisposedAsyncSource.SetResult();

            await result;

            result = uut.DisposeAsync();

            result.IsCompletedSuccessfully.ShouldBeTrue();
        }

        [Test]
        public async Task ManagerIsNotRunning_WaitsForOnDisposedAsync()
        {
            using var seqLoggerManager = new FakeSeqLoggerManager();

            using var serviceProvider = new ServiceCollection()
                .AddSingleton<ISeqLoggerEventChannel>(new FakeSeqLoggerEventChannel())
                .AddSingleton<ISeqLoggerManager>(seqLoggerManager)
                .AddSingleton<ISystemClock>(new FakeSystemClock())
                .BuildServiceProvider();

            var onDisposedAsyncSource = new TaskCompletionSource();
            var mockOnDisposedAsync = new Mock<Func<ValueTask>>();
            mockOnDisposedAsync
                .Setup(x => x())
                .Returns(new ValueTask(onDisposedAsyncSource.Task));

            var uut = new Uut(
                onDisposedAsync:    mockOnDisposedAsync.Object,
                serviceProvider:    serviceProvider);

            uut.SetScopeProvider(new FakeExternalScopeProvider());

            var result = uut.DisposeAsync();

            seqLoggerManager.IsRunning.ShouldBeFalse();

            // Verify that we're waiting for onDisposedAsync().
            mockOnDisposedAsync.Verify(
                x => x(),
                Times.Once);
            result.IsCompleted.ShouldBeFalse();

            onDisposedAsyncSource.SetResult();

            await result;

            result = uut.DisposeAsync();

            result.IsCompletedSuccessfully.ShouldBeTrue();
        }
    }
}
