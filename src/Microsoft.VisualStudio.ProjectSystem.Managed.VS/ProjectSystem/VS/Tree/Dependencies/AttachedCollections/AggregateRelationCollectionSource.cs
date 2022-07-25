// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.ComponentModel;
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

            if (collection is not null)
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
        bool IAsyncAttachedCollectionSource.IsUpdatingHasItems => _collection is null;

        /// <summary>
        /// Sets the backing collection for this source, for cases where that collection was not available at the time
        /// this collection source was constructed.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The collection has already been set, either via this method or via the constructor.
        /// </exception>
        public void SetCollection(IAggregateRelationCollection collection)
        {
            if (_collection is not null)
            {
                throw new InvalidOperationException("Backing collection has already been provided.");
            }

            _collection = Requires.NotNull(collection, nameof(collection));
            _collection.HasItemsChanged += delegate
            {
                PropertyChanged?.Invoke(this, KnownEventArgs.HasItemsPropertyChanged);
            };

            PropertyChanged?.Invoke(this, KnownEventArgs.IsUpdatingItemsPropertyChanged);
            PropertyChanged?.Invoke(this, KnownEventArgs.HasItemsPropertyChanged);
        }
    }
}
