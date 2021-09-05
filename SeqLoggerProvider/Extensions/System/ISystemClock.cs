using System.Threading;
using System.Threading.Tasks;

namespace System
{
    internal interface ISystemClock
    {
        DateTimeOffset Now { get; }

        Task WaitAsync(
            TimeSpan            duration,
            CancellationToken   cancellationToken);
    }
}
