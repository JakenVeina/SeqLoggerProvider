using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.ObjectPool;

namespace SeqLoggerProvider.Internal
{
    internal interface ISeqLoggerEventChannel
    {
        public long EventCount { get; }

        List<object> GetScopeStatesBuffer();

        void ReturnScopeStatesBuffer(List<object> buffer);

        SeqLoggerEvent? TryReadEvent();

        Task WaitForAvailableEventsAsync(CancellationToken cancellationToken);

        void WriteEvent(SeqLoggerEvent @event);
    }

    internal class SeqLoggerEventChannel
        : ISeqLoggerEventChannel
    {
        public SeqLoggerEventChannel()
        {
            _events = Channel.CreateUnbounded<SeqLoggerEvent>(new UnboundedChannelOptions()
            {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = false
            });

            _scopeStatesBufferPool = ObjectPool.Create(new ScopeStatesBufferPooledObjectPolicy());
        }

        public long EventCount
            => _eventCount;

        public List<object> GetScopeStatesBuffer()
            => _scopeStatesBufferPool.Get();

        public void ReturnScopeStatesBuffer(List<object> buffer)
            => _scopeStatesBufferPool.Return(buffer);

        public SeqLoggerEvent? TryReadEvent()
        {
            if (_events.Reader.TryRead(out var @event))
            {
                Interlocked.Decrement(ref _eventCount);
                return @event;
            }
            else
                return null;
        }

        public Task WaitForAvailableEventsAsync(CancellationToken cancellationToken)
            => _events.Reader.WaitToReadAsync(cancellationToken)
                .AsTask();

        public void WriteEvent(SeqLoggerEvent @event)
        {
            // This can never fail, because the channel is unbounded
            _events.Writer.TryWrite(@event);

            Interlocked.Increment(ref _eventCount);
        }

        private readonly Channel<SeqLoggerEvent>    _events;
        private readonly ObjectPool<List<object>>   _scopeStatesBufferPool;

        private long _eventCount;

        private class ScopeStatesBufferPooledObjectPolicy
            : IPooledObjectPolicy<List<object>>
        {
            public List<object> Create()
                => new();

            public bool Return(List<object> obj)
            {
                obj.Clear();

                return true;
            }
        }
    }
}
