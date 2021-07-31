using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SeqLoggerProvider.Internal
{
    internal interface ISeqLoggerEventChannel
    {
        bool TryReadEvent(out SeqLoggerEvent @event);

        Task WaitForAvailableEventsAsync(CancellationToken cancellationToken);

        void WriteEvent(SeqLoggerEvent @event);
    }

    internal class SeqLoggerEventChannel
        : ISeqLoggerEventChannel
    {
        public SeqLoggerEventChannel()
            => _events = Channel.CreateUnbounded<SeqLoggerEvent>(new UnboundedChannelOptions()
            {
                AllowSynchronousContinuations   = true,
                SingleReader                    = true,
                SingleWriter                    = false
            });

        public bool TryReadEvent(out SeqLoggerEvent @event)
            => _events.Reader.TryRead(out @event!);

        public Task WaitForAvailableEventsAsync(CancellationToken cancellationToken)
            => _events.Reader.WaitToReadAsync(cancellationToken)
                .AsTask();

        public void WriteEvent(SeqLoggerEvent @event)
            => _events.Writer.TryWrite(@event);

        private readonly Channel<SeqLoggerEvent> _events;
    }
}
