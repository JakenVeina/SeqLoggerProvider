using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeqLoggerProvider.Utilities
{
    public class DefaultSystemClock
        : ISystemClock
    {
        public DateTimeOffset Now
            => DateTimeOffset.Now;

        public Task WaitAsync(
                TimeSpan            duration,
                CancellationToken   cancellationToken)
            => Task.Delay(duration, cancellationToken);
    }
}
