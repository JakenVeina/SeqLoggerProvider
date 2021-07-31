using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SeqLoggerProvider
{
    public class SeqLoggerConfiguration
    {
        public IReadOnlyDictionary<string, string>? GlobalScopeState { get; init; }

        public int MaxPayloadSize { get; init; }
            = SeqLoggerConstants.DefaultMaxPayloadSize;

        public TimeSpan MaxBatchInterval { get; init; }
            = TimeSpan.FromSeconds(SeqLoggerConstants.DefaultMaxBatchIntervalSeconds);

        public TimeSpan MinBatchInterval { get; init; }
            = TimeSpan.FromSeconds(SeqLoggerConstants.DefaultMinBatchIntervalSeconds);

        [Required]
        public string ServerUrl { get; init; }
            = null!;

        public string? ApiKey { get; init; }
    }
}
