using Microsoft.Extensions.ObjectPool;

namespace SeqLoggerProvider.Internal
{
    internal class SeqLoggerEntryPooledObjectPolicy

        : IPooledObjectPolicy<ISeqLoggerEntry>
    {
        public ISeqLoggerEntry Create()
            => new SeqLoggerEntry();

        public bool Return(ISeqLoggerEntry obj)
        {
            obj.Reset();

            return true;
        }
    }
}
