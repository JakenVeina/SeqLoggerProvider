using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SeqLoggerProvider.Utilities;

namespace SeqLoggerProvider.Internal
{
    [ProviderAlias("Seq")]
    internal sealed class SeqLoggerProvider
        : ILoggerProvider,
            IAsyncDisposable
    {
        public SeqLoggerProvider(
            ISeqLoggerEventChannel  seqLoggerEventChannel,
            IServiceProvider        serviceProvider,
            ISystemClock            systemClock)
        {
            _seqLoggerEventChannel  = seqLoggerEventChannel;
            _serviceProvider        = serviceProvider;
            _systemClock            = systemClock;

            _managerStopTokenSource = new();
        }

        public ILogger CreateLogger(string categoryName)
            => (_externalScopeProvider is null)
                ? throw new InvalidOperationException("The logging provider has not been fully initialized. A scope provider has not been supplied.")
                : new SeqLogger(
                    categoryName,
                    _externalScopeProvider,
                    OnLog,
                    _seqLoggerEventChannel,
                    _systemClock);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
            => _externalScopeProvider = scopeProvider;

        public async ValueTask DisposeAsync()
        {
            _managerStopTokenSource.Cancel();

            if (_whenManagerStopped is not null)
                await _whenManagerStopped;

            _managerStopTokenSource.Dispose();
        }

        void IDisposable.Dispose()
            => DisposeAsync().GetAwaiter().GetResult();

        private void OnLog()
        {
            var isManagerRunning = Interlocked.Exchange(ref _isManagerRunning, 1);
            if (isManagerRunning == 0)
                _whenManagerStopped = Task.Run(() => _serviceProvider.GetRequiredService<ISeqLoggerManager>().RunAsync(_managerStopTokenSource.Token));
        }

        private readonly CancellationTokenSource    _managerStopTokenSource;
        private readonly ISeqLoggerEventChannel     _seqLoggerEventChannel;
        private readonly IServiceProvider           _serviceProvider;
        private readonly ISystemClock               _systemClock;

        private IExternalScopeProvider? _externalScopeProvider;
        private int                     _isManagerRunning; // Using int cause Interlocked doesn't support bool
        private Task?                   _whenManagerStopped;
    }
}
