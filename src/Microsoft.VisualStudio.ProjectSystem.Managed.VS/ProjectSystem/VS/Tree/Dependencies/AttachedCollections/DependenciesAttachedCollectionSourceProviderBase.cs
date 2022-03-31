// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Base class for attaching <see cref="IRelatableItem"/> children to <see cref="IVsHierarchyItem"/>s in the tree.
    /// </summary>
    /// <remarks>
    /// See also <see cref="RelationAttachedCollectionSourceProvider"/> which attaches children and parents
    /// to <see cref="IRelatableItem"/> objects that already exist in the tree.
    /// </remarks>
    public abstract class DependenciesAttachedCollectionSourceProviderBase : IAttachedCollectionSourceProvider
    {
        private readonly FlagsStringMatcher _flagsStringMatcher;

        [Import] private IRelationProvider RelationProvider { get; set; } = null!;

        protected DependenciesAttachedCollectionSourceProviderBase(ProjectTreeFlags flags) => _flagsStringMatcher = new FlagsStringMatcher(flags);

        /// <summary>
        /// Creates collection sources for selected hierarchy items, depending upon the implementation.
        /// </summary>
        /// <remarks>
        /// Only called for hierarchy items whose flags match those passed to the constructor. <paramref name="flagsString"/> is
        /// provided in case further information is needed.
        /// </remarks>
        protected abstract bool TryCreateCollectionSource(
            IVsHierarchyItem hierarchyItem,
            string flagsString,
            string? target,
            IRelationProvider relationProvider,
            [NotNullWhen(returnValue: true)] out AggregateRelationCollectionSource? containsCollectionSource);

        IAttachedCollectionSource? IAttachedCollectionSourceProvider.CreateCollectionSource(object item, string relationName)
        {
            if (relationName == KnownRelationships.Contains)
            {
                if (item is IVsHierarchyItem hierarchyItem)
                {
                    if (hierarchyItem.TryGetFlagsString(out string? flagsString) && _flagsStringMatcher.Matches(flagsString))
                    {
                        hierarchyItem.TryFindTarget(out string? target);

                        if (TryCreateCollectionSource(hierarchyItem, flagsString, target, RelationProvider, out AggregateRelationCollectionSource? containsCollection))
                        {
                            return containsCollection;
                        }
                    }
                }
            }

            return null;
        }

        IEnumerable<IAttachedRelationship> IAttachedCollectionSourceProvider.GetRelationships(object item)
        {
            // Unlike RelationAttachedCollectionSourceProviderBase, this method will not be
            // called for context menus, as the source item is a IVsHierarchyItem in the tree.
            if (item is IRelatableItem)
            {
                yield return Relationships.Contains;
            }
        }
    }
}
