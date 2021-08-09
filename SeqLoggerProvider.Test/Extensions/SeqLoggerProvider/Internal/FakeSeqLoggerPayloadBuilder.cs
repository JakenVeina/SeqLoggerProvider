using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.Extensions.SeqLoggerProvider.Internal
{
    public class FakeSeqLoggerPayloadBuilder
        : ISeqLoggerPayloadBuilder
    {
        public FakeSeqLoggerPayloadBuilder()
        {
            _payloadDatasets = new();
            _waitForPayloadDataSource = new();
        }

        public IReadOnlyList<PayloadDataset> PayloadDatasets
            => _payloadDatasets;

        public bool IsPayloadDataAvailable
            => _payloadDatasets.Count is not 0;

        public void AddPayloadDataset(PayloadDataset dataset)
        {
            _payloadDatasets.Add(dataset);
            _waitForPayloadDataSource.TrySetResult();
        }

        public AppendPayloadDataResult AppendPayloadData(MemoryStream payload)
        {
            if (_payloadDatasets.Count is 0)
                return new()
                {
                    EventsAdded         = 0,
                    IsDeliveryNeeded    = false
                };

            var dataset = _payloadDatasets[0];

            _payloadDatasets.RemoveAt(0);

            if (_payloadDatasets.Count is 0)
                _waitForPayloadDataSource = new();

            foreach(var dataByte in dataset.Data)
                payload.WriteByte(dataByte);

            return dataset.AppendResult;
        }

        public Task WaitForPayloadDataAsync(CancellationToken cancellationToken)
            => Task.WhenAny(
                _waitForPayloadDataSource.Task,
                Task.Delay(Timeout.Infinite, cancellationToken));

        private readonly List<PayloadDataset> _payloadDatasets;

        private TaskCompletionSource _waitForPayloadDataSource;
    }
}
