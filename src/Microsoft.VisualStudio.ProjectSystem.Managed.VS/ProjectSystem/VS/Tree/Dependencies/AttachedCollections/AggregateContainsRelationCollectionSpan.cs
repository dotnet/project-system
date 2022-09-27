// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Represents a subsection within a <see cref="AggregateContainsRelationCollection"/> which is
    /// owned by a particular <see cref="IRelation"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances of this type are owned by their parent <see cref="AggregateContainsRelationCollection"/>
    /// and passed to <see cref="IRelation.UpdateContainsCollection"/> whenever the children of the parent item
    /// must be created or updated.
    /// </para>
    /// <para>
    /// Implementations should pay particular attention to the documentation of <see cref="UpdateContainsItems{TData,TItem}"/>
    /// as it has specific requirements of its arguments in order to perform correctly with good performance.
    /// </para>
    /// </remarks>
    public sealed class AggregateContainsRelationCollectionSpan
    {
        private readonly AggregateContainsRelationCollection _parent;

        internal int BaseIndex { get; set; }
        internal IRelation Relation { get; }
        internal List<IRelatableItem>? Items { get; private set; }

        internal AggregateContainsRelationCollectionSpan(AggregateContainsRelationCollection parent, IRelation relation)
        {
            Requires.NotNull(parent, nameof(parent));
            Requires.NotNull(relation, nameof(relation));

            _parent = parent;
            Relation = relation;
        }

        /// <summary>
        /// Updates the items contained within this span, which ultimately contributes to the
        /// parent <see cref="AggregateContainsRelationCollection"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method runs on the UI thread, recursively across all materialized collections in the tree.
        /// In order to provide linear time complexity, <paramref name="sources"/> must have a stable sorted order across invocations
        /// of this method. Failing this requirement will result in increased numbers of updates to items, degrading performance.
        /// </para>
        /// <para>
        /// This method operates over the ordered sequence of <typeparamref name="TData"/> values in <paramref name="sources"/>,
        /// comparing them each in turn (via <paramref name="comparer"/>) to any existing <typeparamref name="TItem"/> items
        /// in the span. This comparison only considers the 'identity' of its operands, not their state. The comparison determines
        /// what happens for that data/item pair:
        /// <list type="bullet">
        ///   <item>
        ///     <paramref name="comparer"/> returns zero -- the source value and existing item match. <paramref name="update"/> is
        ///     called with both, allowing the data value to update the item in-place. If <paramref name="update"/> returns
        ///     <see langword="true"/> then any materialized descendents of the item are updated recursively via their relations.
        ///   </item>
        ///   <item>
        ///     <paramref name="comparer"/> returns negative -- the source value does not exist in the current collection and should
        ///     be added. <paramref name="factory"/> is called to produce the new item to insert.
        ///   </item>
        ///   <item>
        ///     <paramref name="comparer"/> returns positive -- the source no longer contains the item, and it should be removed.
        ///   </item>
        /// </list>
        /// This method ensures the appropriate <see cref="INotifyCollectionChanged"/> events are triggered on the parent
        /// <see cref="AggregateContainsRelationCollection"/> in response to these updates.
        /// </para>
        /// </remarks>
        /// <param name="sources">
        /// The sequence of data values that the resulting items in this span will reflect when this method completes. The sequence must
        /// be ordered in a way that <paramref name="comparer"/>.
        /// </param>
        /// <param name="comparer">
        /// Compares a source value with an existing item to determine whether they have equivalent identity.
        /// Does not consider the state within either type while producing its result.
        /// </param>
        /// <param name="update">
        /// Updates a tree item based on a data value. Returns <see langword="true"/> is the update mutated the tree item in
        /// some way, otherwise <see langword="false"/>.</param>
        /// <param name="factory">
        /// Creates a new item for a given data value.
        /// </param>
        public void UpdateContainsItems<TData, TItem>(
            IEnumerable<TData> sources,
            Func<TData, TItem, int> comparer,
            Func<TData, TItem, bool> update,
            Func<TData, TItem> factory)
            where TItem : class, IRelatableItem
        {
            using IEnumerator<TData> src = sources.GetEnumerator();

            bool? srcConsumed = null;

            for (int itemIndex = 0; Items is not null && itemIndex < Items.Count; itemIndex++)
            {
                if (srcConsumed != false && !src.MoveNext())
                {
                    // Source stream ended, all remaining items are invalid. Remove each in turn.
                    // Note we do not reset as that reset would refresh the entire parent collection, not just this span of items.
                    // We remove items in reverse order to reduce shuffling items in collections.
                    for (int removeAtIndex = Items.Count - 1; removeAtIndex >= itemIndex; removeAtIndex--)
                    {
                        IRelatableItem removedItem = Items[removeAtIndex];
                        Items.RemoveAt(removeAtIndex);
                        _parent.RaiseChange(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, BaseIndex + removeAtIndex));
                    }

                    srcConsumed = true;
                    break;
                }

                TData source = src.Current;

                var item = (TItem)Items[itemIndex];

                int comparison = comparer(source, item);

                if (comparison == 0)
                {
                    // Items match, update in place
                    if (update(source, item))
                    {
                        // The update changed state, so notify its contains collection to update any materialized children via its relations
                        item.ContainsCollection?.OnStateUpdated();
                    }
                    srcConsumed = true;
                }
                else if (comparison < 0)
                {
                    // Source contains a new item to insert
                    TItem newItem = factory(source);
                    Items.Insert(itemIndex, newItem);
                    _parent.RaiseChange(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, BaseIndex + itemIndex));
                    srcConsumed = true;
                }
                else
                {
                    // Source is missing this item, remove it
                    Items.RemoveAt(itemIndex);
                    _parent.RaiseChange(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, BaseIndex + itemIndex));

                    // decrement the index as we've removed the item from this index and need to consider the one which is now at this index
                    itemIndex--;
                    srcConsumed = false;
                }
            }

            while (srcConsumed == false || src.MoveNext())
            {
                // Add extra source items to end of list
                TData source = src.Current;
                TItem newItem = factory(source);
                Items ??= new List<IRelatableItem>();
                Items.Add(newItem);
                _parent.RaiseChange(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, BaseIndex + Items.Count - 1));
                srcConsumed = true;
            }

            if (Items?.Count == 0)
            {
                Items = null;
            }
        }

        public override string ToString() => $"{Relation.GetType().Name} ({Items?.Count ?? 0})";
    }
}
