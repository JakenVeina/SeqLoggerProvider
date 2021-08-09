using System.Collections.Generic;

namespace SeqLoggerProvider.Test.Extensions.SeqLoggerProvider.Internal
{
    public struct PayloadDelivery
    {
        public IReadOnlyList<byte> Data { get; init; }

        public uint EventCount { get; init; }
    }
}
