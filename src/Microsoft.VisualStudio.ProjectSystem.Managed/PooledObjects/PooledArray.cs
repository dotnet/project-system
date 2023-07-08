// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

#nullable disable

namespace Microsoft.VisualStudio.Buffers.PooledObjects
{
    [DebuggerDisplay("Count = {Count,nq}")]
    [DebuggerTypeProxy(typeof(PooledArray<>.DebuggerProxy))]
    internal sealed partial class PooledArray<T> : IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        private readonly ImmutableArray<T>.Builder _builder;

        private readonly ObjectPool<PooledArray<T>> _pool;

        public PooledArray(int size)
        {
            _builder = ImmutableArray.CreateBuilder<T>(size);
        }

        public PooledArray()
            : this(8)
        { }

        private PooledArray(ObjectPool<PooledArray<T>> pool)
            : this()
        {
            _pool = pool;
        }

        /// <summary>
        /// Realizes the array.
        /// </summary>
        public ImmutableArray<T> ToImmutable()
        {
            return _builder.ToImmutable();
        }

        public int Count
        {
            get
            {
                return _builder.Count;
            }
            set
            {
                _builder.Count = value;
            }
        }

        public T this[int index]
        {
            get
            {
                return _builder[index];
            }

            set
            {
                _builder[index] = value;
            }
        }

        /// <summary>
        /// Write <paramref name="value"/> to slot <paramref name="index"/>.
        /// Fills in unallocated slots preceding the <paramref name="index"/>, if any.
        /// </summary>
        public void SetItem(int index, T value)
        {
            while (index > _builder.Count)
            {
                _builder.Add(default);
            }

            if (index == _builder.Count)
            {
                _builder.Add(value);
            }
            else
            {
                _builder[index] = value;
            }
        }

        public void Add(T item) => _builder.Add(item);

        public void Insert(int index, T item) => _builder.Insert(index, item);

        public void EnsureCapacity(int capacity)
        {
            if (_builder.Capacity < capacity)
            {
                _builder.Capacity = capacity;
            }
        }

        public void Clear() => _builder.Clear();

        public bool Contains(T item) => _builder.Contains(item);

        public int IndexOf(T item) => _builder.IndexOf(item);

        public int IndexOf(T item, IEqualityComparer<T> equalityComparer)
            => _builder.IndexOf(item, 0, _builder.Count, equalityComparer);

        public int IndexOf(T item, int startIndex, int count)
            => _builder.IndexOf(item, startIndex, count);

        public int FindIndex(Predicate<T> match)
            => FindIndex(0, Count, match);

        public int FindIndex(int startIndex, Predicate<T> match)
            => FindIndex(startIndex, Count - startIndex, match);

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(_builder[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public void RemoveAt(int index) => _builder.RemoveAt(index);

        public void RemoveLast() => _builder.RemoveAt(_builder.Count - 1);

        public void ReverseContents() => _builder.Reverse();

        public void Sort() => _builder.Sort();

        public void Sort(IComparer<T> comparer)
        {
            _builder.Sort(comparer);
        }

        public void Sort(Comparison<T> compare)
            => Sort(Comparer<T>.Create(compare));

        public void Sort(int startIndex, IComparer<T> comparer)
            => _builder.Sort(startIndex, _builder.Count - startIndex, comparer);

        public T[] ToArray() => _builder.ToArray();

        public void CopyTo(T[] array, int start) => _builder.CopyTo(array, start);

        public T Last() => _builder[_builder.Count - 1];

        public T First() => _builder[0];

        public bool Any() => _builder.Count > 0;

        /// <summary>
        /// Realizes the array.
        /// </summary>
        public ImmutableArray<T> ToImmutableOrNull()
            => Count == 0 ? default : ToImmutable();

        /// <summary>
        /// Realizes the array, downcasting each element to a derived type.
        /// </summary>
        public ImmutableArray<U> ToDowncastedImmutable<U>()
            where U : T
        {
            if (Count == 0)
            {
                return ImmutableArray<U>.Empty;
            }

            var tmp = PooledArray<U>.GetInstance(Count);
            foreach (T i in this)
            {
                tmp.Add((U)i);
            }

            return tmp.ToImmutableAndFree();
        }

        /// <summary>
        /// Realizes the array and disposes the builder in one operation.
        /// </summary>
        public ImmutableArray<T> ToImmutableAndFree()
        {
            ImmutableArray<T> result;
            if (_builder.Capacity == Count)
            {
                result = _builder.MoveToImmutable();
            }
            else
            {
                result = ToImmutable();
            }

            Free();
            return result;
        }

        public T[] ToArrayAndFree()
        {
            T[] result = ToArray();
            Free();
            return result;
        }

        // To implement Poolable, you need two things:
        // 1) Expose Freeing primitive. 
        public void Free()
        {
            ObjectPool<PooledArray<T>> pool = _pool;
            if (pool is not null)
            {
                // We do not want to retain (potentially indefinitely) very large builders 
                // while the chance that we will need their size is diminishingly small.
                // It makes sense to constrain the size to some "not too small" number. 
                // Overall perf does not seem to be very sensitive to this number, so I picked 128 as a limit.
                if (_builder.Capacity < 128)
                {
                    if (Count != 0)
                    {
                        Clear();
                    }

                    pool.Free(this);
                    return;
                }
            }
        }

        // 2) Expose the pool or the way to create a pool or the way to get an instance.
        //    for now we will expose both and figure which way works better
        private static readonly ObjectPool<PooledArray<T>> s_poolInstance = CreatePool();
        public static PooledArray<T> GetInstance()
        {
            PooledArray<T> builder = s_poolInstance.Allocate();
            return builder;
        }

        public static PooledArray<T> GetInstance(int capacity)
        {
            PooledArray<T> builder = GetInstance();
            builder.EnsureCapacity(capacity);
            return builder;
        }

        public static PooledArray<T> GetInstance(int capacity, T fillWithValue)
        {
            PooledArray<T> builder = GetInstance();
            builder.EnsureCapacity(capacity);

            for (int i = 0; i < capacity; i++)
            {
                builder.Add(fillWithValue);
            }

            return builder;
        }

        public static ObjectPool<PooledArray<T>> CreatePool()
        {
            // We use a default size of 128 objects in the pool
            // This makes it likely that we can handle all use cases
            // even if many consumers require objects from the pool
            // in practice we expect 128 allocated objects in the pool
            // to be rare.  A normal operating set should be around 10.
            return CreatePool(128);
        }

        public static ObjectPool<PooledArray<T>> CreatePool(int size)
        {
            ObjectPool<PooledArray<T>> pool = null;
            pool = new ObjectPool<PooledArray<T>>(() => new PooledArray<T>(pool), size);
            return pool;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal Dictionary<K, ImmutableArray<T>> ToDictionary<K>(Func<T, K> keySelector, IEqualityComparer<K> comparer = null)
        {
            if (Count == 1)
            {
                var dictionary1 = new Dictionary<K, ImmutableArray<T>>(1, comparer);
                T value = this[0];
                dictionary1.Add(keySelector(value), ImmutableArray.Create(value));
                return dictionary1;
            }

            if (Count == 0)
            {
                return new Dictionary<K, ImmutableArray<T>>(comparer);
            }

            // bucketize
            // prevent reallocation. it may not have 'count' entries, but it won't have more. 
            var accumulator = new Dictionary<K, PooledArray<T>>(Count, comparer);
            for (int i = 0; i < Count; i++)
            {
                T item = this[i];
                K key = keySelector(item);
                if (!accumulator.TryGetValue(key, out PooledArray<T> bucket))
                {
                    bucket = GetInstance();
                    accumulator.Add(key, bucket);
                }

                bucket.Add(item);
            }

            var dictionary = new Dictionary<K, ImmutableArray<T>>(accumulator.Count, comparer);

            // freeze
            foreach (KeyValuePair<K, PooledArray<T>> pair in accumulator)
            {
                dictionary.Add(pair.Key, pair.Value.ToImmutableAndFree());
            }

            return dictionary;
        }

        public void AddRange(PooledArray<T> items)
        {
            _builder.AddRange(items._builder);
        }

        public void AddRange<U>(PooledArray<U> items) where U : T
        {
            _builder.AddRange(items._builder);
        }

        public void AddRange(ImmutableArray<T> items)
        {
            _builder.AddRange(items);
        }

        public void AddRange(ImmutableArray<T> items, int length)
        {
            _builder.AddRange(items, length);
        }

        public void AddRange<S>(ImmutableArray<S> items) where S : class, T
        {
            AddRange(ImmutableArray<T>.CastUp(items));
        }

        public void AddRange(T[] items, int start, int length)
        {
            for (int i = start, end = start + length; i < end; i++)
            {
                Add(items[i]);
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            _builder.AddRange(items);
        }

        public void AddRange(params T[] items)
        {
            _builder.AddRange(items);
        }

        public void AddRange(T[] items, int length)
        {
            _builder.AddRange(items, length);
        }

        public void Clip(int limit)
        {
            _builder.Count = limit;
        }

        public void ZeroInit(int count)
        {
            _builder.Clear();
            _builder.Count = count;
        }

        public void AddMany(T item, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Add(item);
            }
        }

        public void RemoveDuplicates()
        {
            var set = PooledHashSet<T>.GetInstance();

            int j = 0;
            for (int i = 0; i < Count; i++)
            {
                if (set.Add(this[i]))
                {
                    this[j] = this[i];
                    j++;
                }
            }

            Clip(j);
            set.Free();
        }

        public ImmutableArray<S> SelectDistinct<S>(Func<T, S> selector)
        {
            var result = PooledArray<S>.GetInstance(Count);
            var set = PooledHashSet<S>.GetInstance();

            foreach (T item in this)
            {
                S selected = selector(item);
                if (set.Add(selected))
                {
                    result.Add(selected);
                }
            }

            set.Free();
            return result.ToImmutableAndFree();
        }
    }
}
