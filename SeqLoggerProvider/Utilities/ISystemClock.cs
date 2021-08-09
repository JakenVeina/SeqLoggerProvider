using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeqLoggerProvider.Utilities
{
    public interface ISystemClock
    {
        DateTimeOffset Now { get; }

        Task WaitAsync(
            TimeSpan            duration,
            CancellationToken   cancellationToken);
    }
}
