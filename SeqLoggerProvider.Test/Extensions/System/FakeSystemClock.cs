using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    internal class FakeSystemClock
        : ISystemClock
    {
        public FakeSystemClock()
            => _waits = new();

        public DateTimeOffset Now
        {
            get => _now;
            set
            {
                _now = value;

                _waits.RemoveAll(wait =>
                {
                    if (wait.source.Task.IsCompleted)
                        return true;

                    if (wait.until <= _now)
                    {
                        wait.source.SetResult();
                        return true;
                    }

                    return false;
                });
            }
        }

        public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken)
        {
            var source = new TaskCompletionSource();

            cancellationToken.Register(() => source.TrySetCanceled(cancellationToken));

            _waits.Add((source, _now + duration));

            return source.Task;
        }

        private readonly List<(TaskCompletionSource source, DateTimeOffset until)> _waits;

        private DateTimeOffset _now;
    }
}
