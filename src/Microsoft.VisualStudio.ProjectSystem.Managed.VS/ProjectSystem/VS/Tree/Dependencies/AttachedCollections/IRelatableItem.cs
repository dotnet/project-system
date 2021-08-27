// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Defines an item in the dependencies tree which has relationships with parent/child items, via which the
    /// tree may be lazily constructed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementors will usually want to derive from <see cref="RelatableItemBase"/> rather than implementing
    /// this interface directly.
    /// </para>
    /// <para>
    /// Enough state must be available on items for their <see cref="IRelation"/>s to create parent and child items.
    /// </para>
    /// </remarks>
    public interface IRelatableItem
    {
        /// <summary>
        /// Gets the "contains" collection for this item, if materialized.
        /// </summary>
        /// <remarks>
        /// This collection holds child items and is lazily constructed by <see cref="TryGetOrCreateContainsCollection"/>,
        /// and lazily populated via <see cref="IAggregateRelationCollection.EnsureMaterialized"/>.
        /// </remarks>
        AggregateContainsRelationCollection? ContainsCollection { get; }

        /// <summary>
        /// Gets and sets the "contained by" collection for this item, if it exists.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This collection holds parent items and is only constructed for items which appear as part of search results.
        /// This includes both items that directly match the search and their ancestors.
        /// </para>
        /// <para>
        /// This collection is set during search. Implementations only need to provide storage for this field, with no
        /// logic in the getter or setter.
        /// </para>
        /// </remarks>
        AggregateContainedByRelationCollection? ContainedByCollection { get; set; }

        /// <summary>
        /// Gets a value that uniquely identifies amongst other items of the same type within the project's target framework.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This value is used to deduplicate items when building ancestral chains of search results. The value may
        /// be anything, so long as two equivalent items have the same value (hash code and equality), and two
        /// different items do not.
        /// </para>
        /// <para>
        /// The item's type and target are implicitly part of this identity, so do not need to be included by implementations.
        /// </para>
        /// </remarks>
        object Identity { get; }

        /// <summary>
        /// Gets the "contains" collection for this item, creating it if necessary. Creation will fail if there
        /// are no relations registered for this item type, in which case there is no need to allocate a collection.
        /// </summary>
        bool TryGetOrCreateContainsCollection(
            IRelationProvider relationProvider,
            [NotNullWhen(returnValue: true)] out AggregateContainsRelationCollection? relationCollection);

        /// <summary>
        /// Attempts to find the <see cref="IProjectTree"/> item that corresponds to the top-level dependency modeled by this item.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method should only be implemented for item types that correspond to top-level project dependencies. All other item
        /// types may <see langword="false"/>.
        /// </para>
        /// <para>
        /// This value is used during search to connection a search result's ancestry to existing tree items.
        /// </para>
        /// </remarks>
        bool TryGetProjectNode(
            IProjectTree targetRootNode,
            IRelatableItem item,
            [NotNullWhen(returnValue: true)] out IProjectTree? projectTree);
    }
}
