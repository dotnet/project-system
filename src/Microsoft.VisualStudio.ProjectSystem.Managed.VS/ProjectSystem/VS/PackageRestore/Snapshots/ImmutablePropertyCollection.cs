// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Abstract immutable collection that supports lookup by index and name. Also supports transforming the
    ///     input items to provide a collection of "output" items of a different type.
    /// </summary>
    /// <typeparam name="T">The type of items presented to consumers of the collection</typeparam>
    /// <typeparam name="U">The type of input items</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    internal abstract class ImmutablePropertyCollection<T, U> : IEnumerable<T> where T : class
    {
        // Data here must be treated as immutable, even if the backing storage is mutable.
        // We ensure no access allows mutation, and a reference to the mutable state cannot escape.
        // We use these simple mutable types for reduction of memory and improved performance.

        private readonly T[] _itemByIndex;
        private readonly IReadOnlyDictionary<string, T> _itemsByName;

        /// <summary>
        ///     Creates a new instance of <see cref="ImmutablePropertyCollection{T, U}"/>.
        /// </summary>
        /// <param name="inputItems">The set of input items</param>
        /// <param name="keyAccessor">A function for mapping from an input item to a key</param>
        /// <param name="itemTransformer">A function for mapping an input item to an output item</param>
        protected ImmutablePropertyCollection(ImmutableArray<U> inputItems, Func<U, string> keyAccessor, Func<U, T> itemTransformer)
        {
            // If the input is empty, don't allocate anything further.
            if (inputItems.Length == 0)
            {
                _itemByIndex = Array.Empty<T>();
                _itemsByName = ImmutableDictionary<string, T>.Empty;
                return;
            }

            // Build an array, to maintain order for index-based lookup.
            var itemByIndex = new T[inputItems.Length];

            // Build a dictionary to support key-based lookup.
            var itemByName = new Dictionary<string, T>(capacity: inputItems.Length);

            int index = 0;

            foreach (U inputItem in inputItems)
            {
                T item = itemTransformer(inputItem);
                itemByIndex[index++] = item;

                string key = keyAccessor(inputItem);

                // While the majority of elements (items, properties, metadata) are guaranteed to be unique,
                // Target Frameworks are not, filter out duplicates - NuGet uses the int-based indexer anyway.
                if (!itemByName.ContainsKey(key))
                {
                    itemByName.Add(key, item);
                }
            }

            _itemByIndex = itemByIndex;
            _itemsByName = itemByName;
        }

        public int Count
        {
            get { return _itemByIndex.Length; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_itemByIndex).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _itemByIndex.GetEnumerator();
        }

        public T? Item(object index)
        {
            return index switch
            {
                string name => GetItemByName(name),
                int intIndex => GetItemByIndex(intIndex),
                _ => throw new ArgumentException(null, nameof(index))
            };

            T? GetItemByName(string name)
            {
                if (_itemsByName.TryGetValue(name, out T? item))
                {
                    return item;
                }

                return null;
            }

            T GetItemByIndex(int index)
            {
                return _itemByIndex[index];
            }
        }
    }
}
