using System.Threading;
using System.Threading.Tasks;

namespace SeqLoggerProvider.Internal
{
    internal class FakeSeqLoggerManager
        : ISeqLoggerManager
    {
        public FakeSeqLoggerManager()
            => _whenStoppedSource = new();

        public bool HasStarted
            => _hasStarted;

        public bool HasStopBeenRequested
            => _hasStopBeenRequested;

        public Task WhenStopped
            => _whenStoppedSource.Task;

        public bool ShouldStop
        {
            get => _shouldStop;
            set
            {
                _shouldStop = value;
                if (_shouldStop && _hasStopBeenRequested)
                    _whenStoppedSource.TrySetResult();
            }
        }

        public void EnsureStarted(CancellationToken stopToken)
        {
            _hasStarted = true;

            stopToken.Register(() =>
            {
                _hasStopBeenRequested = true;
                if (_shouldStop)
                    _whenStoppedSource.TrySetResult();
            });
        }

        private readonly TaskCompletionSource _whenStoppedSource;
     
        private bool _hasStarted;
        private bool _hasStopBeenRequested;
        private bool _shouldStop;
    }
}
