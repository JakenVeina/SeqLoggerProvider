using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider
{
    /// <summary>
    /// Contains compile-time constant values, used by the Seq Logger system.
    /// </summary>
    public static class SeqLoggerConstants
    {
        /// <summary>
        /// The HTTP header name, to be used to attach a Seq API Key to HTTP requests
        /// </summary>
        public const string ApiKeyHeaderName
            = "X-Seq-ApiKey";

        /// <summary>
        /// The default value used for <see cref="SeqLoggerOptions.MaxDeliveryInterval"/>.
        /// </summary>
        public const int DefaultMaxDeliveryIntervalSeconds
            = 10;

        /// <summary>
        /// The default value used for <see cref="SeqLoggerOptions.MaxPayloadSize"/>.
        /// </summary>
        public const uint DefaultMaxPayloadSize
            = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// The default value used for <see cref="SeqLoggerOptions.MinDeliveryInterval"/>.
        /// </summary>
        public const int DefaultMinDeliveryIntervalSeconds
            = 1;

        /// <summary>
        /// The default value used for <see cref="SeqLoggerOptions.PriorityDeliveryLevel"/>.
        /// </summary>
        public const LogLevel DefaultPriorityDeliveryLevel
            = LogLevel.Error;

        /// <summary>
        /// The alias name used when registering <see cref="SeqLoggerProvider"/> instances, within a logging system.
        /// </summary>
        public const string ProviderName
            = "Seq";

        internal const string EventIngestionApiPath
            = "api/events/raw";

        internal const string HttpClientName
            = "SeqLogger";

        internal const string PayloadMediaType
            = "application/vnd.serilog.clef";
    }
}
