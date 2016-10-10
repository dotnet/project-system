// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    /// Abstract list with Item method for getting members by index or name
    /// </summary>
    internal abstract class VsItemList<T> : KeyedCollection<string, T>
    {
        public VsItemList() : base() { }

        public VsItemList(IEnumerable<T> collection) : base()
        {
            Requires.NotNull(collection, nameof(collection));

            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public T Item(Object index)
        {
            try 
            {
                if (index is string)
                {
                    return this[(string)index];
                }
                else if (index is int)
                {
                    return this[(int)index];
                }
            } catch(Exception){}

            return default(T);
        }
    }
}
