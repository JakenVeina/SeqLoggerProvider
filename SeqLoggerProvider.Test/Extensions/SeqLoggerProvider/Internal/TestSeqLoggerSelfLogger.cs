using System;

using Microsoft.Extensions.Logging;

namespace SeqLoggerProvider.Internal
{
    internal sealed class TestSeqLoggerSelfLogger
        : ISeqLoggerSelfLogger,
            IDisposable
    {
        public TestSeqLoggerSelfLogger()
        {
            _loggerFactory = LoggerFactory.Create(builder => builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole());

            _logger = _loggerFactory.CreateLogger<TestSeqLoggerSelfLogger>();
        }

        public bool IsMaterialized
            => _isMaterialized;

        public IDisposable BeginScope<TState>(TState state)
            => _logger.BeginScope(state);

        public void Dispose()
            => _loggerFactory.Dispose();

        public void EnsureMaterialized()
            => _isMaterialized = true;

        public bool IsEnabled(LogLevel logLevel)
            => _logger.IsEnabled(logLevel);

        public void Log<TState>(
                LogLevel                            logLevel,
                EventId                             eventId,
                TState                              state,
                Exception?                          exception,
                Func<TState, Exception?, string>    formatter)
            => _logger.Log(
                logLevel,
                eventId,
                state,
                exception,
                formatter);

        private readonly ILogger        _logger;
        private readonly ILoggerFactory _loggerFactory;

        private bool _isMaterialized;
    }
}
