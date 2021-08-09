using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Options
{
    public class FakeOptionsMonitor<T>
        : IDictionary<string, T>,
            IOptionsMonitor<T>
    {
        public FakeOptionsMonitor()
        {
            _listeners      = new();
            _optionsByName  = new();
        }

        public T this[string name]
        {
            get => _optionsByName[name];
            set => _optionsByName[name] = value;
        }

        public int Count
            => _optionsByName.Count;

        public T CurrentValue
        {
            get => _optionsByName[Options.DefaultName];
            set => _optionsByName[Options.DefaultName] = value;
        }

        public ICollection<string> Names
            => _optionsByName.Keys;

        public ICollection<T> Values
            => _optionsByName.Values;

        public void Add(string name, T value)
            => _optionsByName.Add(name, value);

        public void Clear()
            => _optionsByName.Clear();

        public bool ContainsName(string name)
            => _optionsByName.ContainsKey(name);

        public T Get(string name)
            => _optionsByName[name];

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
            => _optionsByName.GetEnumerator();

        public IDisposable OnChange(Action<T, string> listener)
        {
            _listeners.Add(listener);
            return new Disposal(() => _listeners.Remove(listener));
        }

        public bool Remove(string name)
            => _optionsByName.Remove(name);

        public bool TryGetValue(string name, [MaybeNullWhen(false)] out T value)
            => _optionsByName.TryGetValue(name, out value);

        bool ICollection<KeyValuePair<string, T>>.IsReadOnly
            => false;

        ICollection<string> IDictionary<string, T>.Keys
            => _optionsByName.Keys;

        void ICollection<KeyValuePair<string, T>>.Add(KeyValuePair<string, T> item)
            => ((ICollection<KeyValuePair<string, T>>)_optionsByName).Add(item);

        bool ICollection<KeyValuePair<string, T>>.Contains(KeyValuePair<string, T> item)
            => ((ICollection<KeyValuePair<string, T>>)_optionsByName).Contains(item);

        bool IDictionary<string, T>.ContainsKey(string key)
            => _optionsByName.ContainsKey(key);

        void ICollection<KeyValuePair<string, T>>.CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<string, T>>)_optionsByName).CopyTo(array, arrayIndex);

        IEnumerator IEnumerable.GetEnumerator()
            => _optionsByName.GetEnumerator();

        bool ICollection<KeyValuePair<string, T>>.Remove(KeyValuePair<string, T> item)
            => ((ICollection<KeyValuePair<string, T>>)_optionsByName).Remove(item);

        private readonly List<Action<T, string>>    _listeners;
        private readonly Dictionary<string, T>      _optionsByName;

        private sealed class Disposal
            : IDisposable
        {
            public Disposal(Action onDispose)
                => _onDispose = onDispose;

            public void Dispose()
                => _onDispose.Invoke();

            private readonly Action _onDispose;
        }
    }
}
