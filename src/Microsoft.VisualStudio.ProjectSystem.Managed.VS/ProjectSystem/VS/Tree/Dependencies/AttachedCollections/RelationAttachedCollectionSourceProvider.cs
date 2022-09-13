// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Attaches parents/children to <see cref="IRelatableItem"/> instances via <see cref="IRelation"/>s.
    /// </summary>
    /// <remarks>
    /// See also <see cref="DependenciesAttachedCollectionSourceProviderBase"/> which attaches children
    /// to the <see cref="IVsHierarchyItem"/> objects that represent top-level project dependencies.
    /// </remarks>
    [AppliesToProject(ProjectCapability.DependenciesTree)]
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(RelationAttachedCollectionSourceProvider))]
    [VisualStudio.Utilities.Order(Before = HierarchyItemsProviderNames.Contains)]
    internal sealed class RelationAttachedCollectionSourceProvider : IAttachedCollectionSourceProvider
    {
        [Import] private IRelationProvider RelationProvider { get; set; } = null!;

        public IAttachedCollectionSource? CreateCollectionSource(object item, string relationName)
        {
            if (relationName == KnownRelationships.Contains)
            {
                if (item is IRelatableItem relatableItem)
                {
                    if (relatableItem.TryGetOrCreateContainsCollection(RelationProvider, out AggregateContainsRelationCollection? collection))
                    {
                        return new AggregateRelationCollectionSource(relatableItem, collection);
                    }
                }
            }
            else if (relationName == KnownRelationships.ContainedBy)
            {
                if (item is IRelatableItem { ContainedByCollection: { } containedByCollection } relatableItem)
                {
                    return new AggregateRelationCollectionSource(relatableItem, containedByCollection);
                }
            }

            return null;
        }

        public IEnumerable<IAttachedRelationship> GetRelationships(object item)
        {
            if (item is IRelatableItem relatableItem)
            {
                // We always return "Contains" relationship, even if no IRelation exists to produce
                // those children. Doing so lights up context menu items related to Solution Explorer
                // scoping.
                yield return Relationships.Contains;

                if (relatableItem.ContainedByCollection is not null)
                {
                    yield return Relationships.ContainedBy;
                }
            }
        }
    }
}
