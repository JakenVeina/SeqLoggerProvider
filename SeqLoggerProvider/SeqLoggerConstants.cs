namespace SeqLoggerProvider
{
    public static class SeqLoggerConstants
    {
        public const string JsonSerializerOptionsName
            = "SeqLogger";

        internal const int DefaultMaxBatchIntervalSeconds
            = 10;

        internal const int DefaultMaxPayloadSize
            = 10 * 1024 * 1024; // 10MB

        internal const int DefaultMinBatchIntervalSeconds
            = 1;

        internal const string HttpClientName
            = "SeqLogger";

        internal const string PayloadMediaType
            = "application/vnd.serilog.clef";
    }
}
