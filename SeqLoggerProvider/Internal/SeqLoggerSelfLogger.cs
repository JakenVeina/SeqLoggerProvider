using System;
using System.Threading;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider.Internal
{
    internal interface ISeqLoggerSelfLogger
        : ILogger
    {
        void EnsureMaterialized();
    }

    internal class SeqLoggerSelfLogger
        : ISeqLoggerSelfLogger
    {
        public SeqLoggerSelfLogger(Func<ILogger> loggerFactory)
            => _loggerFactory = loggerFactory;

        public IDisposable BeginScope<TState>(TState state)
            => Logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel)
            => Logger.IsEnabled(logLevel);

        public void Log<TState>(
                LogLevel                            logLevel,
                EventId                             eventId,
                TState                              state,
                Exception?                          exception,
                Func<TState, Exception?, string>    formatter)
            => Logger.Log(
                logLevel,
                eventId,
                state,
                exception,
                formatter);

        public void EnsureMaterialized()
        {
            var hasMaterializeStarted = Interlocked.Exchange(ref _hasMaterializeStarted, 1);
            if (hasMaterializeStarted is not 0)
                return;

            _logger = _loggerFactory.Invoke();
        }

        private ILogger Logger
            => _logger ?? throw new InvalidOperationException("The self-logger must be materialized before it can be used.");

        private readonly Func<ILogger> _loggerFactory;

        private int         _hasMaterializeStarted; // Interlocked doesn't support bool
        private ILogger?    _logger;
    }
}
