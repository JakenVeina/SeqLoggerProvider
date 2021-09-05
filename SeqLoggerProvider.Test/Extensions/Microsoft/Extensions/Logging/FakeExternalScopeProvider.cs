using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging
{
    public class FakeExternalScopeProvider
        : IExternalScopeProvider
    {
        public IReadOnlyDictionary<IDisposable, object?> ActiveStatesByDisposal
            => _statesByDisposal;

        public void ForEachScope<TState>(
            Action<object?, TState> callback,
            TState                  state)
        {
            foreach (var scopeState in _statesByDisposal.Values)
                callback.Invoke(scopeState, state);
        }

        public IDisposable Push(object? state)
        {
            var disposal = new Disposal(disposal => _statesByDisposal.Remove(disposal));

            _statesByDisposal.Add(disposal, state);

            return disposal;
        }

        private readonly Dictionary<IDisposable, object?> _statesByDisposal
            = new();
    }
}
