// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Implementation of <see cref="IAttachedCollectionSource"/> that aggregates items provided by multiple
    /// <see cref="IRelation"/>s for a given source item.
    /// </summary>
    /// <remarks>
    /// Supports delayed construction of the backing <see cref="IAggregateRelationCollection"/> instance, for
    /// cases where a collection is requested immediately via a synchronous call to
    /// <see cref="IAttachedCollectionSourceProvider.CreateCollectionSource"/> on the UI thread, but the data
    /// required for the collection must be obtained asynchronously.
    /// </remarks>
    public sealed class AggregateRelationCollectionSource : IAsyncAttachedCollectionSource
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly object _sourceItem;
        private IAggregateRelationCollection? _collection;

        public AggregateRelationCollectionSource(object sourceItem, IAggregateRelationCollection? collection = null)
        {
            _sourceItem = Requires.NotNull(sourceItem, nameof(sourceItem));

            if (collection != null)
            {
                SetCollection(collection);
            }
        }

        object IAttachedCollectionSource.SourceItem => _sourceItem;

        IEnumerable? IAttachedCollectionSource.Items
        {
            get
            {
                // We are being asked for items, so materialize them if they have not yet been
                _collection?.EnsureMaterialized();
                return _collection;
            }
        }

        // This can be computed without materializing items (by querying the item's relations)
        bool IAttachedCollectionSource.HasItems => _collection?.HasItems == true;

        // We are updating items until they are provided
        bool IAsyncAttachedCollectionSource.IsUpdatingHasItems => _collection == null;

        /// <summary>
        /// Sets the backing collection for this source, for cases where that collection was not available at the time
        /// this collection source was constructed.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The collection has already been set, either via this method or via the constructor.
        /// </exception>
        public void SetCollection(IAggregateRelationCollection collection)
        {
            if (_collection != null)
            {
                throw new InvalidOperationException("Backing collection has already been provided.");
            }

            _collection = Requires.NotNull(collection, nameof(collection));
            _collection.HasItemsChanged += delegate
            {
                PropertyChanged?.Invoke(this, KnownEventArgs.HasItemsPropertyChanged);
            };

            // Work around inconsistent use of IPrioritizedComparable in the tree view control
            // https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1158136
            if (CollectionViewSource.GetDefaultView(_collection) is ListCollectionView view)
            {
                view.CustomSort = PrioritizedComparableComparer.Instance;
            }

            PropertyChanged?.Invoke(this, KnownEventArgs.IsUpdatingItemsPropertyChanged);
            PropertyChanged?.Invoke(this, KnownEventArgs.HasItemsPropertyChanged);
        }

        private sealed class PrioritizedComparableComparer : IComparer
        {
            public static IComparer Instance { get; } = new PrioritizedComparableComparer();

            public int Compare(object x, object y)
            {
                if (x == null || y == null)
                {
                    // We don't expect nulls
                    return 0;
                }

                // Handle prioritized comparison
                if (x is IPrioritizedComparable comparable1 && y is IPrioritizedComparable comparable2)
                {
                    int order = comparable1.Priority.CompareTo(comparable2.Priority);

                    if (order != 0)
                    {
                        return order;
                    }

                    return comparable1.CompareTo(comparable2);
                }

                // Handle non-prioritized comparison
                if (x is IComparable item1 && y is IComparable item2)
                {
                    int order = item1.CompareTo(item2);

                    if (order != 0)
                    {
                        return order;
                    }
                }

                return 0;
            }
        }
    }
}
