namespace System
{
    internal sealed class Disposal
        : IDisposable
    {
        public Disposal(Action<Disposal> onDisposed)
            => _onDisposed = onDisposed;

        public void Dispose()
            => _onDisposed.Invoke(this);

        private readonly Action<Disposal> _onDisposed;
    }
}
