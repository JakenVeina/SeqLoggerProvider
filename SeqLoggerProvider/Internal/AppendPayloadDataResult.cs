namespace SeqLoggerProvider.Internal
{
    public struct AppendPayloadDataResult
    {
        public uint EventsAdded { get; init; }

        public bool IsDeliveryNeeded { get; init; }
    }
}
