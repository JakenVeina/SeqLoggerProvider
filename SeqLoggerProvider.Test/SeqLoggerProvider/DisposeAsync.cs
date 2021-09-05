using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Moq;
using NUnit.Framework;
using Shouldly;

namespace SeqLoggerProvider.Test.SeqLoggerProvider
{
    [TestFixture]
    public class DisposeAsync
    {
        [Test]
        public async Task ManagerIsNotRunning_CompletesImmediately()
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            var result = uut.DisposeAsync();

            result.IsCompleted.ShouldBeTrue();

            await result;

            testContext.Manager.HasStarted.ShouldBeFalse();
            testContext.Manager.HasStopBeenRequested.ShouldBeFalse();
        }

        [Test]
        public async Task ManagerIsRunning_WaitsForManagerToStop()
        {
            using var testContext = new TestContext();
            testContext.Manager.ShouldStop = false;

            var uut = testContext.BuildUut();

            uut.SetScopeProvider(Mock.Of<IExternalScopeProvider>());

            var logger = uut.CreateLogger("CategoryName");
            logger.Log(
                logLevel:   LogLevel.Debug,
                eventId:    new EventId(1, "Event"),
                state:      default(object?),
                exception:  null,
                formatter:  (_, _) => "");

            var result = uut.DisposeAsync();

            result.IsCompleted.ShouldBeFalse();

            testContext.Manager.HasStopBeenRequested.ShouldBeTrue();
            testContext.Manager.ShouldStop = true;

            result.IsCompleted.ShouldBeTrue();

            await result;
        }

        [Test]
        public async Task DisposeHasStarted_CompletesImmediately()
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            uut.SetScopeProvider(Mock.Of<IExternalScopeProvider>());

            _ = uut.CreateLogger("CategoryName");

            await uut.DisposeAsync();

            testContext.Manager.ShouldStop = false;

            var result = uut.DisposeAsync();

            result.IsCompleted.ShouldBeTrue();

            await result;
        }
    }
}
