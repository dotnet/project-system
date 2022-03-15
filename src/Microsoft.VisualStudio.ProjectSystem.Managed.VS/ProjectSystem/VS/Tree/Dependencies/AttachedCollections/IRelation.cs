// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Models a bidirectional relationship between a parent and child items,
    /// where the parent may contain children, and a child is contained by parents.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Extension, Cardinality = ImportCardinality.ZeroOrMore)]
    public interface IRelation
    {
        /// <summary>
        /// Determines whether this relation can produce contained (child) items for <paramref name="parentType"/>.
        /// </summary>
        bool SupportsContainsFor(Type parentType);

        /// <summary>
        /// Determines whether this relation can produce containing (parent) items for <paramref name="childType"/>.
        /// </summary>
        bool SupportsContainedByFor(Type childType);

        /// <summary>
        /// Gets whether this relation will produce at least one child item for <paramref name="parent"/>.
        /// </summary>
        bool HasContainedItem(IRelatableItem parent);

        /// <summary>
        /// Updates the child items of <paramref name="parent"/> in <paramref name="span"/>.
        /// </summary>
        /// <remarks>
        /// Implementations should call <see cref="AggregateContainsRelationCollectionSpan.UpdateContainsItems{TData,TItem}"/>
        /// and provide callbacks specific to the particular relation.
        /// </remarks>
        void UpdateContainsCollection(IRelatableItem parent, AggregateContainsRelationCollectionSpan span);

        /// <summary>
        /// Creates the set of parent items that contains <paramref name="child"/>.
        /// </summary>
        /// <remarks>
        /// Parent collections are only used in search. Search does not support dynamically updating results
        /// in the tree, so unlike <see cref="UpdateContainsCollection"/>, implementations of this method are
        /// only concerned with the one-time construction of parent items.
        /// </remarks>
        IEnumerable<IRelatableItem>? CreateContainedByItems(IRelatableItem child);
    }
}
