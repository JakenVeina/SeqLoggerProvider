using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Logging;

using BatchingLoggerProvider;

namespace SeqLoggerProvider
{
    /// <summary>
    /// Describes a set of options for controlling the behavior of a <see cref="SeqLoggerProvider"/> instance.
    /// </summary>
    public class SeqLoggerOptions
        : BatchingLoggerOptions
    {
        /// <summary>
        /// The API key (if any) needed to access the Seq server.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// A set of fields to be attached to every log event that is delivered to the server.
        /// </summary>
        public IReadOnlyDictionary<string, string>? GlobalFields { get; set; }

        /// <summary>
        /// The maximum size (in bytes) of payloads to be sent to the server.
        /// </summary>
        public int MaxPayloadSize { get; set; }
            = SeqLoggerConstants.DefaultMaxPayloadSize;

        /// <summary>
        /// The log severity level which will trigger a "priority delivery" of log events to the server. That is, when an event of this level, or higher, occurs, it triggers the provider to immediately deliver all pending logs to the server.
        /// </summary>
        public LogLevel PriorityDeliveryLevel { get; set; }
            = SeqLoggerConstants.DefaultPriorityDeliveryLevel;

        /// <summary>
        /// The URL of the Seq server, to which log events are to be sent.
        /// </summary>
        [Required]
        public string ServerUrl { get; set; }
            = null!;
    }
}
