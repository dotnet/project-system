// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Buffers.PooledObjects
{
    // Dictionary that can be recycled via an object pool
    // NOTE: these dictionaries always have the default comparer.
    internal class PooledDictionary<K, V> : Dictionary<K, V>
        where K : notnull
    {
        private readonly ObjectPool<PooledDictionary<K, V>> _pool;

        private PooledDictionary(ObjectPool<PooledDictionary<K, V>> pool)
        {
            _pool = pool;
        }

        public ImmutableDictionary<K, V> ToImmutableDictionaryAndFree()
        {
            var result = this.ToImmutableDictionary();
            Free();
            return result;
        }

        public void Free()
        {
            Clear();
            _pool?.Free(this);
        }

        // global pool
        private static readonly ObjectPool<PooledDictionary<K, V>> s_poolInstance = CreatePool();

        // if someone needs to create a pool;
        public static ObjectPool<PooledDictionary<K, V>> CreatePool()
        {
            ObjectPool<PooledDictionary<K, V>>? pool = null;
            pool = new ObjectPool<PooledDictionary<K, V>>(() => new PooledDictionary<K, V>(pool!), 128);
            return pool;
        }

        public static PooledDictionary<K, V> GetInstance()
        {
            PooledDictionary<K, V> instance = s_poolInstance.Allocate();
            // Debug.Assert(instance.Count == 0);
            return instance;
        }
    }
}
