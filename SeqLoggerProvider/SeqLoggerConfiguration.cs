using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider
{
    public class SeqLoggerConfiguration
    {
        public string? ApiKey { get; set; }

        public IReadOnlyDictionary<string, string>? GlobalFields { get; set; }

        public TimeSpan MaxDeliveryInterval { get; set; }
            = TimeSpan.FromSeconds(SeqLoggerConstants.DefaultMaxDeliveryIntervalSeconds);

        public int MaxPayloadSize { get; set; }
            = SeqLoggerConstants.DefaultMaxPayloadSize;

        public TimeSpan MinDeliveryInterval { get; set; }
            = TimeSpan.FromSeconds(SeqLoggerConstants.DefaultMinDeliveryIntervalSeconds);

        public LogLevel PriorityDeliveryLevel { get; set; }
            = LogLevel.Error;

        [Required]
        public string ServerUrl { get; set; }
            = null!;
    }
}
