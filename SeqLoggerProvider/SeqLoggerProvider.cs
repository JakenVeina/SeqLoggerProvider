using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SeqLoggerProvider.Internal;
using SeqLoggerProvider.Utilities;

namespace SeqLoggerProvider
{
    [ProviderAlias("Seq")]
    public sealed class SeqLoggerProvider
        : ILoggerProvider,
            ISupportExternalScope,
            IAsyncDisposable
    {
        internal SeqLoggerProvider(
            Func<ValueTask>     onDisposedAsync,
            IServiceProvider    serviceProvider)
        {
            _onDisposedAsync    = onDisposedAsync;
            _serviceProvider    = serviceProvider;

            _managerStopTokenSource = new();
        }

        public ILogger CreateLogger(string categoryName)
            => (_externalScopeProvider is null)
                ? throw new InvalidOperationException("The logging provider has not been fully initialized. A scope provider has not been supplied.")
                : new SeqLogger(
                    categoryName,
                    _externalScopeProvider,
                    OnLog,
                    _serviceProvider.GetRequiredService<ISeqLoggerEventChannel>(),
                    _serviceProvider.GetRequiredService<ISystemClock>());

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
            => _externalScopeProvider = scopeProvider;

        public async ValueTask DisposeAsync()
        {
            if (_hasDisposed)
                return;

            _managerStopTokenSource.Cancel();

            if (_whenManagerStopped is not null)
                await _whenManagerStopped;

            _managerStopTokenSource.Dispose();

            await _onDisposedAsync.Invoke();

            _hasDisposed = true;
        }

        void IDisposable.Dispose()
        {
            var result = DisposeAsync();
            if (!result.IsCompleted)
                result.AsTask().GetAwaiter().GetResult();
        }

        private void OnLog()
        {
            var isManagerRunning = Interlocked.Exchange(ref _isManagerRunning, 1);
            if (isManagerRunning == 0)
            {
                // Create the manager immediately, rather than on the background thread.
                // This prevents a race condition for very-short-lived providers.
                var manager = _serviceProvider.GetRequiredService<ISeqLoggerManager>();
                _whenManagerStopped = Task.Run(() => manager.RunAsync(_managerStopTokenSource.Token));
            }
        }

        private readonly CancellationTokenSource    _managerStopTokenSource;
        private readonly Func<ValueTask>            _onDisposedAsync;
        private readonly IServiceProvider           _serviceProvider;

        private IExternalScopeProvider? _externalScopeProvider;
        private bool                    _hasDisposed;
        private int                     _isManagerRunning; // Using int cause Interlocked doesn't support bool
        private Task?                   _whenManagerStopped;
    }
}
