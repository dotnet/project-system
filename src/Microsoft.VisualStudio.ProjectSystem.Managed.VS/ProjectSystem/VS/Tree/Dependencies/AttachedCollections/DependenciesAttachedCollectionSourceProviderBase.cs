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
        private readonly ProjectTreeFlags _flags;

        [Import] private IRelationProvider RelationProvider { get; set; } = null!;

        protected DependenciesAttachedCollectionSourceProviderBase(ProjectTreeFlags flags) => _flags = flags;

        /// <summary>
        /// Creates a collection source for a hierarchy item.
        /// </summary>
        /// <remarks>
        /// Only called for hierarchy items whose flags match those passed to the constructor.
        /// </remarks>
        /// <param name="hierarchyItem">The VS hierarchy item to create a collection for.</param>
        /// <param name="unused">Legacy parameter, no longer provided. Will always be an empty string.</param>
        /// <param name="target">A string that identifies the target framework, for multi-targeting projects.</param>
        /// <param name="relationProvider">The relation provider.</param>
        /// <param name="containsCollectionSource">The returned collection source.</param>
        protected abstract bool TryCreateCollectionSource(
            IVsHierarchyItem hierarchyItem,
            string unused,
            string? target,
            IRelationProvider relationProvider,
            [NotNullWhen(returnValue: true)] out AggregateRelationCollectionSource? containsCollectionSource);

        IAttachedCollectionSource? IAttachedCollectionSourceProvider.CreateCollectionSource(object item, string relationName)
        {
            if (relationName == KnownRelationships.Contains)
            {
                if (item is IVsHierarchyItem hierarchyItem)
                {
                    if (hierarchyItem.TryGetFlags(out ProjectTreeFlags flags) && flags.Contains(_flags))
                    {
                        hierarchyItem.TryFindTarget(out string? target);

                        // NOTE historically we used to pass a string having all project tree flags concatenated
                        // in a single string. Nothing actually uses this value, and it's expensive to create.
                        // Unfortunately this is a public API and the signature of the method cannot change.
                        // So instead, we always just pass an empty string.
                        string flagsString = "";

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
