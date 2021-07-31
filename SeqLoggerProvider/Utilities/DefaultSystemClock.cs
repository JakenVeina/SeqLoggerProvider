using System;

namespace SeqLoggerProvider.Utilities
{
    internal class DefaultSystemClock
        : ISystemClock
    {
        public DateTimeOffset UtcNow
            => DateTimeOffset.UtcNow;
    }
}
