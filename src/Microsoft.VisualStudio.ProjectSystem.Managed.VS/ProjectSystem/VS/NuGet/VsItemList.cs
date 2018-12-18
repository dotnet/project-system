// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    /// Abstract list with Item method for getting members by index or name
    /// </summary>
    internal abstract class VsItemList<T> : KeyedCollection<string, T>
        where T : class
    {
        protected VsItemList() : base() { }

        protected VsItemList(IEnumerable<T> collection) : base()
        {
            Requires.NotNull(collection, nameof(collection));

            foreach (T item in collection)
            {
                Add(item);
            }
        }

        public T? Item(object index)
        {
            if (index is string)
            {
                TryGetValue((string)index, out T? value);
                return value;
            }

            return this[(int)index];
        }

        public bool TryGetValue(string key, [NotNullWhenTrue]out T? value)
        {
            // Until we have https://github.com/dotnet/corefx/issues/4690

            Requires.NotNull(key, nameof(key));

            if (Dictionary != null)
            {
                return Dictionary.TryGetValue(key, out value);
            }

            foreach (T item in Items)
            {
                if (Comparer.Equals(GetKeyForItem(item), key))
                {
                    value = item;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
