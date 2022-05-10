// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft;

namespace System.Collections.Concurrent
{
    internal class ConcurrentHashSet<T> : IConcurrentHashSet<T>
    {
        private static readonly object s_hashSetObject = new();

        private readonly ConcurrentDictionary<T, object> _map;

        public ConcurrentHashSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        public ConcurrentHashSet(IEqualityComparer<T> comparer)
        {
            _map = new(comparer);
        }

        public int Count => _map.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return _map.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ConcurrentHashSet<T>)this).GetEnumerator();
        }

        public bool Add(T item)
        {
            return _map.TryAdd(item, s_hashSetObject);
        }

        public bool AddRange(IEnumerable<T> elements)
        {
            Requires.NotNull(elements, nameof(elements));

            bool changed = false;
            foreach (var element in elements)
            {
                changed |= _map.TryAdd(element, s_hashSetObject);
            }

            return changed;
        }

        public bool Contains(T item)
        {
            return _map.ContainsKey(item);
        }

        public bool Remove(T item)
        {
            return _map.TryRemove(item, out _);
        }

        public void Clear()
        {
            _map.Clear();   
        }
    }
}
