using System;

using Moq;
using NUnit.Framework;
using Shouldly;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider.Test.SeqLoggerProvider
{
    [TestFixture]
    public class Dispose
    {
        [Test]
        public void Always_InvokesDisposeAsync()
        {
            using var testContext = new TestContext();

            var uut = testContext.BuildUut();

            uut.SetScopeProvider(Mock.Of<IExternalScopeProvider>());

            var logger = uut.CreateLogger("CategoryName");
            logger.Log(
                logLevel:   LogLevel.Debug,
                eventId:    new EventId(1, "Event"),
                state:      default(object?),
                exception:  null,
                formatter:  (_, _) => "");

            var mockOnDisposed = new Mock<Action>();
            uut.Disposed += mockOnDisposed.Object;

            ((IDisposable)uut).Dispose();

            testContext.Manager.HasStopBeenRequested.ShouldBeTrue();

            mockOnDisposed.Verify(
                x => x.Invoke(),
                Times.Once);
        }
    }
}
