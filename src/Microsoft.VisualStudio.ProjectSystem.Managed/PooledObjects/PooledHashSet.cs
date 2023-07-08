// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Buffers.PooledObjects
{
    // HashSet that can be recycled via an object pool
    // NOTE: these HashSets always have the default comparer.
    internal class PooledHashSet<T> : HashSet<T>
    {
        private readonly ObjectPool<PooledHashSet<T>> _pool;

        private PooledHashSet(ObjectPool<PooledHashSet<T>> pool)
        {
            _pool = pool;
        }

        public void Free()
        {
            Clear();
            _pool?.Free(this);
        }

        // global pool
        private static readonly ObjectPool<PooledHashSet<T>> s_poolInstance = CreatePool();

        // if someone needs to create a pool;
        public static ObjectPool<PooledHashSet<T>> CreatePool()
        {
            ObjectPool<PooledHashSet<T>>? pool = null;
            pool = new ObjectPool<PooledHashSet<T>>(() => new PooledHashSet<T>(pool!), 128);
            return pool;
        }

        public static PooledHashSet<T> GetInstance()
        {
            PooledHashSet<T> instance = s_poolInstance.Allocate();
            return instance;
        }
    }
}
