using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    public class FakeChannelWriter<T>
        : ChannelWriter<T>
    {
        public FakeChannelWriter()
            => _items = new();

        public IReadOnlyList<T> Items
            => _items;

        public override bool TryWrite(T item)
        {
            _items.Add(item);
            return true;
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
            => new(true);

        private readonly List<T> _items;
    }
}
