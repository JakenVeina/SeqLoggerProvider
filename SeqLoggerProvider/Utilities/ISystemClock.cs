using System;

namespace SeqLoggerProvider.Utilities
{
    internal interface ISystemClock
    {
        public DateTimeOffset UtcNow { get; }
    }
}
