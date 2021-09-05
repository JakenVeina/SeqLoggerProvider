using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.ObjectPool
{
    public class FakeObjectPool<T>
            : ObjectPool<T>
        where T : class
    {
        public FakeObjectPool(Func<T>? objectFactory = null)
        {
            _objectFactory = objectFactory ?? (() => throw new NotSupportedException());

            _createdObjects     = new();
            _returnedObjects    = new();
        }

        public IReadOnlyList<T> CreatedObjects
            => _createdObjects;

        public IReadOnlyList<T> ReturnedObjects
            => _returnedObjects;

        public void Clear()
        {
            _createdObjects.Clear();
            _returnedObjects.Clear();
        }

        public override T Get()
        {
            var obj = _objectFactory.Invoke();
            _createdObjects.Add(obj);
            return obj;
        }

        public override void Return(T obj)
            => _returnedObjects.Add(obj);

        private readonly List<T> _createdObjects;
        private readonly Func<T> _objectFactory;
        private readonly List<T> _returnedObjects;
    }
}
