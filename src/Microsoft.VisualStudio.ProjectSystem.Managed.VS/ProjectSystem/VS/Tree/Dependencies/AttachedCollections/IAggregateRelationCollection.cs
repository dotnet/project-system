// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Windows.Data;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Defines a lazily constructed collection of items which are related to some other item.
    /// This collection can identify whether it contains items before actually materializing them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dependencies tree extenders should not need to implement this interface, instead:
    /// <list type="bullet">
    ///   <item>Use <see cref="AggregateContainsRelationCollectionSpan"/> for children of a parent</item>
    ///   <item>Use <see cref="AggregateContainedByRelationCollection"/> for parents of a child</item>
    /// </list>
    /// </para>
    /// <para>
    /// This collection is "aggregate" in the sense that it contains a collection of items per <see cref="IRelation"/>
    /// that applies to its source <see cref="IRelatableItem"/>.
    /// </para>
    /// <para>
    /// This interface extends <see cref="IList"/> which allows efficient use by <see cref="ListCollectionView"/>.
    /// </para>
    /// </remarks>
    public interface IAggregateRelationCollection : IList
    {
        /// <summary>
        /// Raised whenever <see cref="HasItems"/> changes.
        /// </summary>
        /// <remarks>
        /// Used by <see cref="AggregateRelationCollectionSource"/> to trigger changes to its
        /// <see cref="IAttachedCollectionSource.HasItems"/> property.
        /// </remarks>
        event EventHandler HasItemsChanged;

        /// <summary>
        /// Gets whether this collection contains items. The return value can be computed without materializing items.
        /// This allows expansion indicators to be displayed correctly without actually instantiating items.
        /// </summary>
        bool HasItems { get; }

        /// <summary>
        /// Materializes the items of this collection. If items have already been materialized,
        /// this method returns immediately.
        /// </summary>
        void EnsureMaterialized();
    }
}
