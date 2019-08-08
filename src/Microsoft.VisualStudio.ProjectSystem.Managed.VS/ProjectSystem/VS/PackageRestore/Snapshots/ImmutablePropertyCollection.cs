// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Abstract immutable collection that supports lookup by index and name.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    internal abstract class ImmutablePropertyCollection<T> : IEnumerable<T> where T : class
    {
        private readonly IImmutableList<T> _items;
        private readonly IImmutableDictionary<string, T> _itemsByName;

        protected ImmutablePropertyCollection(IEnumerable<T> items)
        {
            // While the majority of elements (items, properties, metadata) are guaranteed to be unique,
            // Target Frameworks are not, filter out duplicates - NuGet uses the int-based indexer anyway.

#pragma warning disable IDE0039 // We want to cache the delegate
            Func<T, string> keySelector = i => GetKeyForItem(i);
#pragma warning restore IDE0039

            _items = ImmutableList.CreateRange(items);
            _itemsByName = items.Distinct(keySelector, StringComparers.ItemNames)
                                .ToImmutableDictionary(keySelector, StringComparers.ItemNames);
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

        protected abstract string GetKeyForItem(T item);

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
