using System.Collections.Generic;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider.Test.Extensions.SeqLoggerProvider.Internal
{
    public struct PayloadDataset
    {
        public AppendPayloadDataResult AppendResult { get; init; }

        public IReadOnlyList<byte> Data { get; init; }
    }
}
