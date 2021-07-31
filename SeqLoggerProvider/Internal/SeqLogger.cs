using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using SeqLoggerProvider.Utilities;

namespace SeqLoggerProvider.Internal
{
    internal class SeqLogger
        : ILogger
    {
        public SeqLogger(
            string                  categoryName,
            IExternalScopeProvider  externalScopeProvider,
            Action                  onLog,
            ISeqLoggerEventChannel  seqLoggerEventChannel,
            ISystemClock            systemClock)
        {
            _categoryName           = categoryName;
            _externalScopeProvider  = externalScopeProvider;
            _onLog                  = onLog;
            _seqLoggerEventChannel  = seqLoggerEventChannel;
            _systemClock            = systemClock;
        }

        public IDisposable BeginScope<TState>(TState state)
            => _externalScopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(
            LogLevel                            logLevel,
            EventId                             eventId,
            TState                              state,
            Exception?                          exception,
            Func<TState, Exception?, string>    formatter)
        {
            var scopeStates = new List<object>();

            _externalScopeProvider.ForEachScope(
                _scopeStateCollector,
                scopeStates);

            _seqLoggerEventChannel.WriteEvent(new SeqLoggerEvent<TState>(
                categoryName:   _categoryName,
                eventId:        eventId,
                exception:      exception,
                formatter:      formatter,
                logLevel:       logLevel,
                scopeStates:    scopeStates,
                state:          state,
                occurred:      _systemClock.UtcNow));

            _onLog.Invoke();
        }

        private static readonly Action<object?, List<object>> _scopeStateCollector
        = (scopeState, scopeStates) =>
        {
            if (scopeState is not null)
                scopeStates.Add(scopeState);
        };

        private readonly string                 _categoryName;
        private readonly IExternalScopeProvider _externalScopeProvider;
        private readonly Action                 _onLog;
        private readonly ISeqLoggerEventChannel _seqLoggerEventChannel;
        private readonly ISystemClock           _systemClock;
    }
}
