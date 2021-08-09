using System;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;
using Shouldly;

using Uut = SeqLoggerProvider.Utilities.DefaultSystemClock;

namespace SeqLoggerProvider.Test.Utilities.DefaultSystemClock
{
    [TestFixture]
    public class WaitAsync
    {
        [Test]
        public async Task CancellationIsNotRequested_WaitsForDuration()
        {
            var uut = new Uut();

            var start = DateTimeOffset.Now;
            var duration = TimeSpan.FromSeconds(1);

            await uut.WaitAsync(duration, CancellationToken.None);

            DateTimeOffset.Now.ShouldBeGreaterThanOrEqualTo(start + duration);
        }

        [Test]
        public async Task CancellationIsRequested_ThrowsException()
        {
            var uut = new Uut();

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Should.ThrowAsync<TaskCanceledException>(async () =>
            {
                await uut.WaitAsync(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
            });
        }
    }
}
