namespace SeqLoggerProvider
{
    public static class SeqLoggerConstants
    {
        public const string ApiKeyHeaderName
            = "X-Seq-ApiKey";

        public const string HttpClientName
            = "SeqLogger";

        public const string JsonSerializerOptionsName
            = "SeqLogger";

        internal const int DefaultMaxDeliveryIntervalSeconds
            = 10;

        internal const int DefaultMaxPayloadSize
            = 10 * 1024 * 1024; // 10MB

        internal const int DefaultMinDeliveryIntervalSeconds
            = 1;

        internal const string EventIngestionApiPath
            = "api/events/raw";

        internal const string PayloadMediaType
            = "application/vnd.serilog.clef";
    }
}
