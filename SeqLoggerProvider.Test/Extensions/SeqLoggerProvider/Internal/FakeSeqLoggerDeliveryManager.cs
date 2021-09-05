using System.Collections.Generic;
using System.Threading.Tasks;

namespace SeqLoggerProvider.Internal
{
    internal class FakeSeqLoggerDeliveryManager
        : ISeqLoggerDeliveryManager
    {
        public FakeSeqLoggerDeliveryManager()
            => _deliveredPayloads = new();

        public IReadOnlyList<ISeqLoggerPayload> DeliveredPayloads
            => _deliveredPayloads;

        public bool ShouldCompleteDeliveries
        {
            get => _deliveryCompletionSource is null;
            set
            {
                if (value)
                {
                    _deliveryCompletionSource?.SetResult();
                    _deliveryCompletionSource = null;
                }
                else if (_deliveryCompletionSource is null)
                    _deliveryCompletionSource = new();
            }
        }

        public void Clear()
            => _deliveredPayloads.Clear();

        public async Task DeliverAsync(ISeqLoggerPayload payload)
        {
            _deliveredPayloads.Add(payload);

            if (_deliveryCompletionSource is not null)
                await _deliveryCompletionSource.Task;
        }

        private readonly List<ISeqLoggerPayload> _deliveredPayloads;

        private TaskCompletionSource? _deliveryCompletionSource;
    }
}
