using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

using SeqLoggerProvider.Internal;

namespace SeqLoggerProvider
{
    /// <summary>
    /// An implementation of <see cref="ILoggerProvider"/> for a logger that delivers log data to a remote Seq server, in batches.
    /// </summary>
    [ProviderAlias(SeqLoggerConstants.ProviderName)]
    public class SeqLoggerProvider
        : ILoggerProvider,
            ISupportExternalScope,
            IAsyncDisposable
    {
        internal SeqLoggerProvider(
            ChannelWriter<ISeqLoggerEntry>  entryChannelWriter,
            ObjectPool<ISeqLoggerEntry>     entryPool,
            IOptions<JsonSerializerOptions> jsonSerializerOptions,
            ISeqLoggerManager               manager,
            IOptions<SeqLoggerOptions>      options,
            ISeqLoggerSelfLogger            selfLogger,
            ISystemClock                    systemClock)
        {
            _entryChannelWriter     = entryChannelWriter;
            _entryPool              = entryPool;
            _jsonSerializerOptions  = jsonSerializerOptions;
            _manager                = manager;
            _options                = options;
            _selfLogger             = selfLogger;
            _systemClock            = systemClock;

            _managerStopTokenSource = new();
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            var logger = (_scopeProvider is null)
                ? throw new InvalidOperationException("The provider is not fully initialized: SetScopeProvider() has not been called.")
                : new SeqLogger(
                    categoryName:           categoryName,
                    entryChannelWriter:     _entryChannelWriter,
                    entryPool:              _entryPool,
                    jsonSerializerOptions:  _jsonSerializerOptions,
                    logger:                 _selfLogger,
                    options:                _options,
                    scopeProvider:          _scopeProvider,
                    systemClock:            _systemClock);

            // We have post-initialization work to do, and this is the safest place to do it.
            // We can't do it in the constructor or ServiceProvider setup, since it will result in recursion through ILoggerFactory.
            // The same goes for doing it just in CreateLogger() directly.
            if (!_manager.HasStarted)
                logger.CreatingLogEntry += OnCreatingLogEntry;

            return logger;

            void OnCreatingLogEntry()
            {
                _selfLogger.EnsureMaterialized();
                _manager.EnsureStarted(_managerStopTokenSource.Token);
                logger.CreatingLogEntry -= OnCreatingLogEntry;
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_hasDisposeStarted)
                return;
            _hasDisposeStarted = true;

            if (_manager.HasStarted)
            {
                _managerStopTokenSource.Cancel();
                await _manager.WhenStopped;
            }

            _managerStopTokenSource.Dispose();

            _disposed?.Invoke();
        }

        /// <inheritdoc/>
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
            => _scopeProvider = scopeProvider;

        internal event Action Disposed
        {
            add     => _disposed = (Action)Delegate.Combine(_disposed,  value);
            remove  => _disposed = (Action)Delegate.Remove(_disposed,   value);
        }

        void IDisposable.Dispose()
        {
            var result = DisposeAsync();
            if (!result.IsCompletedSuccessfully)
                result.AsTask().GetAwaiter().GetResult();
        }

        private readonly ChannelWriter<ISeqLoggerEntry>     _entryChannelWriter;
        private readonly ObjectPool<ISeqLoggerEntry>        _entryPool;
        private readonly IOptions<JsonSerializerOptions>    _jsonSerializerOptions;
        private readonly ISeqLoggerManager                  _manager;
        private readonly CancellationTokenSource            _managerStopTokenSource;
        private readonly IOptions<SeqLoggerOptions>         _options;
        private readonly ISeqLoggerSelfLogger               _selfLogger;
        private readonly ISystemClock                       _systemClock;

        private Action?                 _disposed;
        private bool                    _hasDisposeStarted;
        private IExternalScopeProvider? _scopeProvider;
    }
}
