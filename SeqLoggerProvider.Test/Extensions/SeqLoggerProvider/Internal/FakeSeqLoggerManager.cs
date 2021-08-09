using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeqLoggerProvider.Internal
{
    public sealed class FakeSeqLoggerManager
        : ISeqLoggerManager,
            IDisposable
    {
        public FakeSeqLoggerManager()
        {
            _whenStartedSource          = new();
            _whenStoppedSource          = new();
            _whenStopRequestedSource    = new();
        }

        public Task WhenStarted
            => _whenStartedSource.Task;

        public Task WhenStopRequested
            => _whenStopRequestedSource.Task;

        public bool IsRunning
            => _isRunning;

        public bool IsStopRequested
            => _isStopRequested;

        public Task RunAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
                throw new InvalidOperationException("The manager is already running");

            _isRunning = true;
            _whenStartedSource.SetResult();
            _whenStoppedSource = new();

            cancellationToken.Register(() =>
            {
                _isStopRequested = true;
                _whenStopRequestedSource.SetResult();
            });

            return _whenStoppedSource.Task;
        }

        public void Dispose()
        {
            _whenStartedSource.TrySetCanceled();
            _whenStoppedSource.TrySetCanceled();
        }

        public void Stop()
        {
            _whenStoppedSource.SetResult();
            _isRunning = false;
            _whenStartedSource = new();
            _isStopRequested = false;
            _whenStopRequestedSource = new();
        }

        private bool                    _isRunning;
        private bool                    _isStopRequested;
        private TaskCompletionSource    _whenStartedSource;
        private TaskCompletionSource    _whenStoppedSource;
        private TaskCompletionSource    _whenStopRequestedSource;
    }
}
