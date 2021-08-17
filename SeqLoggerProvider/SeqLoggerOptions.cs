using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Logging;

using BatchingLoggerProvider;

namespace SeqLoggerProvider
{
    public class SeqLoggerOptions
        : BatchingLoggerOptions
    {
        public string? ApiKey { get; set; }

        public IReadOnlyDictionary<string, string>? GlobalFields { get; set; }

        public int MaxPayloadSize { get; set; }
            = SeqLoggerConstants.DefaultMaxPayloadSize;

        public LogLevel PriorityDeliveryLevel { get; set; }
            = SeqLoggerConstants.DefaultPriorityDeliveryLevel;

        [Required]
        public string ServerUrl { get; set; }
            = null!;
    }
}
