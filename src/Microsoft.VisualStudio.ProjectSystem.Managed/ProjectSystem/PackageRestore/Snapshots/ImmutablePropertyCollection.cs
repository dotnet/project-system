// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Abstract immutable collection that supports lookup by index and name.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    internal abstract class ImmutablePropertyCollection<T> : IEnumerable<T> where T : class
    {
        // Data here must be treated as immutable. We use readonly interfaces into mutable backing types
        // for the reduction in memory and improvements in performance.

        private readonly IReadOnlyList<T> _items;
        private readonly IReadOnlyDictionary<string, T> _itemsByName;

        protected ImmutablePropertyCollection(IEnumerable<T> items, Func<T, string> keyAccessor)
        {
            // Build a list, to maintain order for index-based lookup.
            var itemList = new List<T>();

            // Build a dictionary to support key-based lookup.
            var itemByName = new Dictionary<string, T>();

            foreach (T item in items)
            {
                itemList.Add(item);

                string key = keyAccessor(item);

                // While the majority of elements (items, properties, metadata) are guaranteed to be unique,
                // Target Frameworks are not, filter out duplicates - NuGet uses the int-based indexer anyway.
                if (!itemByName.ContainsKey(key))
                {
                    itemByName.Add(key, item);
                }
            }

            _items = itemList;
            _itemsByName = itemByName;
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public T? Item(object index)
        {
            return index switch
            {
                string name => GetItemByName(name),
                int intIndex => GetItemByIndex(intIndex),
                _ => throw new ArgumentException(null, nameof(index))
            };
        }

        private T? GetItemByName(string name)
        {
            if (_itemsByName.TryGetValue(name, out T? item))
            {
                return item;
            }

            return null;
        }

        private T GetItemByIndex(int index)
        {
            return _items[index];
        }
    }
}
