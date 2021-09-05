using System;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace SeqLoggerProvider.Internal
{
    internal sealed class SeqLogger
        : ILogger
    {
        public SeqLogger(
            string                          categoryName,
            ChannelWriter<ISeqLoggerEntry>  entryChannelWriter,
            ObjectPool<ISeqLoggerEntry>     entryPool,
            IOptions<JsonSerializerOptions> jsonSerializerOptions,
            ISeqLoggerSelfLogger            logger,
            IOptions<SeqLoggerOptions>      options,
            IExternalScopeProvider          scopeProvider,
            ISystemClock                    systemClock)
        {
            _categoryName           = categoryName;
            _eventChannelWriter     = entryChannelWriter;
            _eventPool              = entryPool;
            _jsonSerializerOptions  = jsonSerializerOptions;
            _logger                 = logger;
            _options                = options;
            _scopeProvider          = scopeProvider;
            _systemClock            = systemClock;
        }

        public event Action CreatingLogEntry
        {
            add     => _creatingLogEntry = (Action)Delegate.Combine(_creatingLogEntry,  value);
            remove  => _creatingLogEntry = (Action)Delegate.Remove(_creatingLogEntry,   value);
        }
        private Action? _creatingLogEntry;

        public IDisposable BeginScope<TState>(TState state)
            => _scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel)
            => logLevel is not LogLevel.None;

        public void Log<TState>(
            LogLevel                            logLevel,
            EventId                             eventId,
            TState                              state,
            Exception?                          exception,
            Func<TState, Exception?, string>    formatter)
        {
            var occurredUtc = _systemClock.Now.UtcDateTime;

            _creatingLogEntry?.Invoke();

            var @event = _eventPool.Get();
            try
            {
                @event.Load(
                    categoryName:   _categoryName,
                    eventId:        eventId,
                    exception:      exception,
                    formatter:      formatter,
                    globalFields:   _options.Value.GlobalFields,
                    logLevel:       logLevel,
                    occurredUtc:    occurredUtc,
                    scopeProvider:  _scopeProvider,
                    state:          state,
                    options:        _jsonSerializerOptions.Value);
            }
            catch (Exception ex)
            {
                SeqLoggerLoggerMessages.EntrySerializationFailed(_logger, _categoryName, eventId, logLevel, occurredUtc, ex);

                _eventPool.Return(@event);
                return;
            }

            // This should never fail, since the channel is unbounded
            _eventChannelWriter.TryWrite(@event);
        }

        private readonly string                             _categoryName;
        private readonly ChannelWriter<ISeqLoggerEntry>     _eventChannelWriter;
        private readonly ObjectPool<ISeqLoggerEntry>        _eventPool;
        private readonly IOptions<JsonSerializerOptions>    _jsonSerializerOptions;
        private readonly ISeqLoggerSelfLogger               _logger;
        private readonly IOptions<SeqLoggerOptions>         _options;
        private readonly IExternalScopeProvider             _scopeProvider;
        private readonly ISystemClock                       _systemClock;
    }
}
