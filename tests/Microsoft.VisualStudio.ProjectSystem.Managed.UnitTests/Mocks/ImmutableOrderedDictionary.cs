// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal sealed class ImmutableOrderedDictionary<TKey, TValue>
        : IImmutableDictionary<TKey, TValue>,
          IDataWithOriginalSource<KeyValuePair<TKey, TValue>>
    {
        public static ImmutableOrderedDictionary<TKey, TValue> Empty { get; } = new(Enumerable.Empty<KeyValuePair<TKey, TValue>>());

        private readonly Dictionary<TKey, TValue> _dic;

        public ImmutableOrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            _dic = new();

            foreach ((TKey key, TValue value) in pairs)
            {
                _dic.Add(key, value);
            }
        }

        public IReadOnlyCollection<KeyValuePair<TKey, TValue>> SourceData => _dic.ToList();

        public TValue this[TKey key] => _dic[key];
        public IEnumerable<TKey> Keys => _dic.Keys;
        public IEnumerable<TValue> Values => _dic.Values;
        public int Count => _dic.Count;
        public bool Contains(KeyValuePair<TKey, TValue> pair) => _dic.Contains(pair);
        public bool ContainsKey(TKey key) => _dic.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _dic.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => _dic.GetEnumerator();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dic.GetEnumerator();

        public bool TryGetKey(TKey equalKey, out TKey actualKey) => throw new NotImplementedException();
        public IImmutableDictionary<TKey, TValue> Add(TKey key, TValue value) => throw new NotImplementedException();
        public IImmutableDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs) => throw new NotImplementedException();
        public IImmutableDictionary<TKey, TValue> Clear() => throw new NotImplementedException();
        public IImmutableDictionary<TKey, TValue> Remove(TKey key) => throw new NotImplementedException();
        public IImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys) => throw new NotImplementedException();
        public IImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value) => throw new NotImplementedException();
        public IImmutableDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items) => throw new NotImplementedException();
    }
}
