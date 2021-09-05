using System;

using Microsoft.Extensions.Logging;

using Moq;
using NUnit.Framework;

using Uut = SeqLoggerProvider.Internal.SeqLoggerSelfLogger;

namespace SeqLoggerProvider.Test.Internal.SeqLoggerSelfLogger
{
    [TestFixture]
    public class EnsureMaterialized
    {
        [Test]
        public void LoggerHasNotMaterialized_MaterializesLogger()
        {
            var mockLoggerFactory = new Mock<Func<ILogger>>();

            var uut = new Uut(mockLoggerFactory.Object);

            uut.EnsureMaterialized();

            mockLoggerFactory.Verify(
                x => x.Invoke(),
                Times.Once);
        }

        [Test]
        public void LoggerHasMaterialized_DoesNothing()
        {
            var mockLoggerFactory = new Mock<Func<ILogger>>();

            var uut = new Uut(mockLoggerFactory.Object);

            uut.EnsureMaterialized();
            mockLoggerFactory.Invocations.Clear();

            uut.EnsureMaterialized();

            mockLoggerFactory.Verify(
                x => x.Invoke(),
                Times.Never);
        }
    }
}
