using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    public class FakeChannelReader<T>
        : ChannelReader<T>
    {
        public FakeChannelReader()
        {
            _completionSource   = new();
            _items              = new();

            _currentWaitSource = new();
        }

        public override bool CanCount
            => true;

        public override Task Completion
            => _completionSource.Task;

        public override int Count
            => _items.Count;

        public IReadOnlyList<T> Items
            => _items;

        public int WaitCount
            => _waitCount;

        public void AddItem(T item)
        {
            _items.Add(item);
            _currentWaitSource.TrySetResult();
        }

        public void AddItems(IEnumerable<T> items)
        {
            _items.AddRange(items);

            if (_items.Count is not 0)
                _currentWaitSource.TrySetResult();
        }

        public void Complete()
            => _completionSource.TrySetResult();

        public override bool TryRead([MaybeNullWhen(false)] out T item)
        {
            if (_items.Count is 0)
            {
                item = default;
                return false;
            }

            item = _items[0];
            _items.RemoveAt(0);

            if (_items.Count is 0)
                _currentWaitSource = new();

            return true;
        }

        public override async ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            if (_items.Count is not 0)
                return true;

            if (_completionSource.Task.IsCompleted)
                return false;

            ++_waitCount;

            await await Task.WhenAny(
                _completionSource.Task,
                _currentWaitSource.Task,
                Task.Delay(Timeout.Infinite, cancellationToken));

            --_waitCount;

            return (_items.Count is not 0) || !_completionSource.Task.IsCompleted;
        }

        private readonly TaskCompletionSource   _completionSource;
        private readonly List<T>                _items;

        private TaskCompletionSource    _currentWaitSource;
        private int                     _waitCount;
    }
}
