using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.Extensions.SeqLoggerProvider.Internal
{
    public class FakeSeqLoggerEventDeliveryManager
        : ISeqLoggerEventDeliveryManager
    {
        public FakeSeqLoggerEventDeliveryManager()
        {
            _canDeliver = true;
            _deliveries = new();
        }

        public bool CanDeliver
        {
            get => _canDeliver;
            set => _canDeliver = value;
        }

        public void ClearDeliveries()
            => _deliveries.Clear();

        public IReadOnlyList<PayloadDelivery> Deliveries
            => _deliveries;

        public Task<bool> TryDeliverAsync(MemoryStream payload, uint eventCount)
        {
            if (!_canDeliver)
                return Task.FromResult(false);

            _deliveries.Add(new()
            {
                Data        = payload.GetBuffer().Take((int)payload.Length).ToArray(),
                EventCount  = eventCount
            });

            return Task.FromResult(true);
        }

        private readonly List<PayloadDelivery> _deliveries;

        private bool _canDeliver;
    }
}
