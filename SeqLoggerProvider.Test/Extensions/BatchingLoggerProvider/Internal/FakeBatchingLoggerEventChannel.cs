using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BatchingLoggerProvider.Internal
{
    public class FakeBatchingLoggerEventChannel<TEvent>
            : IBatchingLoggerEventChannel<TEvent>
        where TEvent : class, IBatchingLoggerEvent
    {
        public FakeBatchingLoggerEventChannel()
        {
            _events                         = new();
            _returnedScopeStateBuffers      = new();
            _waitForAvailableEventsSource   = new();
        }

        public long EventCount
            => _events.Count;

        public IReadOnlyList<TEvent> Events
            => _events;

        public List<List<object>> ReturnedScopeStateBuffers
            => _returnedScopeStateBuffers;

        public int TryReadEventInvocationCount
        {
            get => _tryReadEventInvocationCount;
            set => _tryReadEventInvocationCount = value;
        }

        public void ClearEvents()
            => _events.Clear();

        public List<object> GetScopeStatesBuffer()
            => new();

        public void ReturnScopeStatesBuffer(List<object> buffer)
            => _returnedScopeStateBuffers.Add(buffer);

        public TEvent? TryReadEvent()
        {
            ++TryReadEventInvocationCount;

            if (_events.Count is 0)
                return null;

            var @event = _events[0];
            _events.RemoveAt(0);

            if (_events.Count is 0)
                _waitForAvailableEventsSource = new();

            return @event;
        }

        public Task WaitForAvailableEventsAsync(CancellationToken cancellationToken)
            => Task.WhenAny(
                _waitForAvailableEventsSource.Task,
                Task.Delay(Timeout.Infinite, cancellationToken));

        public void WriteEvent(TEvent @event)
        {
            _events.Add(@event);
            _waitForAvailableEventsSource.TrySetResult();
        }

        private readonly List<TEvent>       _events;
        private readonly List<List<object>> _returnedScopeStateBuffers;

        private int                     _tryReadEventInvocationCount;
        private TaskCompletionSource    _waitForAvailableEventsSource;
    }
}
