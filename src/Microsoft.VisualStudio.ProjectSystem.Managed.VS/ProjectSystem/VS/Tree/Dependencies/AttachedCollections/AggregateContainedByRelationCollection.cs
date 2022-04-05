// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// An immutable collection of "contained-by" (parent) items for an <see cref="IRelatableItem"/>,
    /// aggregated across it's potentially many <see cref="IRelation"/>s. Eagerly populated. Used as
    /// part of Solution Explorer search.
    /// </summary>
    public sealed class AggregateContainedByRelationCollection : IAggregateRelationCollection
    {
        /// <summary>
        /// <see cref="HasItems"/> doesn't change for this collection type.
        /// </summary>
        event EventHandler IAggregateRelationCollection.HasItemsChanged { add { } remove { } }

        private readonly List<object> _parentItems;

        internal AggregateContainedByRelationCollection(List<object> parentItems)
        {
            Requires.NotNull(parentItems, nameof(parentItems));

            _parentItems = parentItems;
        }

        public bool HasItems => _parentItems.Count != 0;

        void IAggregateRelationCollection.EnsureMaterialized() {}

        public IEnumerator GetEnumerator() => _parentItems.GetEnumerator();

        public int Count => _parentItems.Count;

        public bool Contains(object value) => _parentItems.Contains(value);

        public int IndexOf(object value) => _parentItems.IndexOf(value);

        object IList.this[int index]
        {
            get => _parentItems[index];
            set => throw new NotSupportedException();
        }

        void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        object ICollection.SyncRoot => throw new NotSupportedException();
        bool ICollection.IsSynchronized => false;
        int IList.Add(object value) => throw new NotSupportedException();
        void IList.Clear() => throw new NotSupportedException();
        void IList.Insert(int index, object value) => throw new NotSupportedException();
        void IList.Remove(object value) => throw new NotSupportedException();
        void IList.RemoveAt(int index) => throw new NotSupportedException();
        bool IList.IsReadOnly => true;
        bool IList.IsFixedSize => true;
    }
}
