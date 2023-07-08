// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace System.Collections.Concurrent
{
    internal interface IConcurrentHashSet<T> : IEnumerable<T>
    {
        /// <summary>
        /// The number of elements contained in the System.Collections.Concurrent.IConcurrentHashSet`1.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Adds an item to the System.Collections.Concurrent.IConcurrentHashSet`1.
        /// </summary>
        /// <param name="item">The object to add to the System.Collections.Concurrent.IConcurrentHashSet`1</param>
        /// <returns>
        /// <c>true</c> if the item was added to the System.Collections.Concurrent.IConcurrentHashSet`1; otherwise, <c>false</c>
        /// </returns>
        bool Add(T item);

        /// <summary>
        /// Determines whether the System.Collections.Concurrent.IConcurrentHashSet`1 contains the specified item.
        /// </summary>
        /// <param name="item">The item to locate in the System.Collections.Concurrent.IConcurrentHashSet`1.</param>
        /// <returns>
        /// <c>true</c> if the System.Collections.Concurrent.IConcurrentHashSet`1 contains the item; otherwise, <c>false</c>.
        /// </returns>
        bool Contains(T item);

        /// <summary>
        /// Removes a specific object from the System.Collections.Concurrent.IConcurrentHashSet`1
        /// </summary>
        /// <param name="item">The object to remove from the System.Collections.Concurrent.IConcurrentHashSet`1.</param>
        /// <returns>
        /// <c>true</c> if item was successfully removed from the System.Collections.Concurrent.IConcurrentHashSet`1;
        /// otherwise, <c>false</c>. This method also returns <c>false</c> if item is not found in the
        /// System.Collections.Concurrent.IConcurrentHashSet`1.
        /// </returns>
        bool Remove(T item);

        /// <summary>
        /// Adds all the elements in a sequence to the set.
        /// </summary>
        /// <param name="elements">The objects to add to the System.Collections.Concurrent.IConcurrentHashSet`1</param>
        /// <returns>
        /// <c>true</c> if at least one item was added to the System.Collections.Concurrent.IConcurrentHashSet`1;
        /// otherwise, <c>false</c>
        /// </returns>
        bool AddRange(IEnumerable<T> elements);

        /// <summary>
        /// Removes all elements from System.Collections.Concurrent.IConcurrentHashSet`1
        /// </summary>
        void Clear();
    }
}
