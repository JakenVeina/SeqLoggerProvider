using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider
{
    public static class SeqLoggerConstants
    {
        public const string ApiKeyHeaderName
            = "X-Seq-ApiKey";

        public const LogLevel DefaultPriorityDeliveryLevel
            = LogLevel.Error;

        public const string ProviderName
            = "Seq";

        internal const int DefaultMaxPayloadSize
            = 10 * 1024 * 1024; // 10MB

        internal const string EventIngestionApiPath
            = "api/events/raw";

        internal const string HttpClientName
            = "SeqLogger";

        internal const string PayloadMediaType
            = "application/vnd.serilog.clef";
    }
}
