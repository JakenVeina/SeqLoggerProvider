using System.Threading;
using System.Threading.Tasks;

namespace System
{
    internal class DefaultSystemClock
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
