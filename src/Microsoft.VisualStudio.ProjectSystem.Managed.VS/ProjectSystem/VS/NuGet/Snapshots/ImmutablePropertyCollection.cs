// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Abstract immutable collection that supports lookup by index and name.
    /// </summary>
    internal abstract class ImmutablePropertyCollection<T> : IEnumerable where T : class
    {
        private readonly IImmutableList<T> _items;
        private readonly IImmutableDictionary<string, T> _itemsByName;

        protected ImmutablePropertyCollection(IEnumerable<T> items)
        {
            Requires.NotNull(items, nameof(items));

            _items = ImmutableList.CreateRange(items);
            _itemsByName = _items.ToImmutableDictionary(i => GetKeyForItem(i));
        }

        public int Count
        {
            get { return _items.Count; }
        }
        public IEnumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        protected abstract string GetKeyForItem(T item);

        public T Item(object index)
        {
            if (index is string name)
            {
                return GetItemByName(name);
            }
            else if (index is int intIndex)
            {
                return GetItemByIndex(intIndex);
            }

            throw new ArgumentException(null, nameof(index));
        }

        private T GetItemByName(string name)
        {
            if (_itemsByName.TryGetValue(name, out T value))
            {
                return value;
            }

            return null;
        }

        private T GetItemByIndex(int index)
        {
            return _items[index];
        }
    }
}
