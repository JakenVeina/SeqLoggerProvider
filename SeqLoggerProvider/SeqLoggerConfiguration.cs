using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider
{
    public class SeqLoggerConfiguration
    {
        public string? ApiKey { get; init; }

        public IReadOnlyDictionary<string, string>? GlobalFields { get; init; }

        public TimeSpan MaxDeliveryInterval { get; init; }
            = TimeSpan.FromSeconds(SeqLoggerConstants.DefaultMaxDeliveryIntervalSeconds);

        public int MaxPayloadSize { get; init; }
            = SeqLoggerConstants.DefaultMaxPayloadSize;

        public TimeSpan MinDeliveryInterval { get; init; }
            = TimeSpan.FromSeconds(SeqLoggerConstants.DefaultMinDeliveryIntervalSeconds);

        public LogLevel PriorityDeliveryLevel { get; init; }
            = LogLevel.Error;

        [Required]
        public string ServerUrl { get; init; }
            = null!;
    }
}
