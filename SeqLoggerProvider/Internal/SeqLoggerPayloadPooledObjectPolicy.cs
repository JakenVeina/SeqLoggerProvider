using Microsoft.Extensions.ObjectPool;

namespace SeqLoggerProvider.Internal
{
    internal class SeqLoggerPayloadPooledObjectPolicy
        : IPooledObjectPolicy<ISeqLoggerPayload>
    {
        public ISeqLoggerPayload Create()
            => new SeqLoggerPayload();

        public bool Return(ISeqLoggerPayload obj)
        {
            obj.Reset();

            return true;
        }
    }
}
