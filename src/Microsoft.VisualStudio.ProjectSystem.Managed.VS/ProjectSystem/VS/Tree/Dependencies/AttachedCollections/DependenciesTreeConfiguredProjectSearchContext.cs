// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    internal sealed class DependenciesTreeConfiguredProjectSearchContext : IDependenciesTreeConfiguredProjectSearchContext
    {
        private readonly DependenciesTreeSearchContext _inner;
        private readonly IVsHierarchyItemManager _hierarchyItemManager;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IRelationProvider _relationProvider;
        private readonly IProjectTree _targetRootNode;

        public DependenciesTreeConfiguredProjectSearchContext(
            DependenciesTreeSearchContext inner,
            IProjectTree targetRootNode,
            IVsHierarchyItemManager hierarchyItemManager,
            IUnconfiguredProjectVsServices projectVsServices,
            IRelationProvider relationProvider)
        {
            _inner = inner;
            _hierarchyItemManager = hierarchyItemManager;
            _projectVsServices = projectVsServices;
            _relationProvider = relationProvider;
            _targetRootNode = targetRootNode;
        }

        public CancellationToken CancellationToken => _inner.CancellationToken;

        public bool IsMatch(string candidateText) => _inner.IsMatch(candidateText);

        private readonly Dictionary<object, IRelatableItem> _itemByKey = new();

        public void SubmitResult(IRelatableItem? item)
        {
            if (item is null)
            {
                return;
            }

            item = DeduplicateItem(item);

            PopulateAncestors(item);

            _inner.SubmitResult(item);

            void PopulateAncestors(IRelatableItem childItem)
            {
                if (childItem.ContainedByCollection is not null)
                {
                    // We've already populated this item's ancestors. It's likely an ancestor of
                    // another search result. This also prevents runaway in case of cycles.
                    return;
                }

                ImmutableArray<IRelation> containedByRelations = _relationProvider.GetContainedByRelationsFor(childItem.GetType());

                if (containedByRelations.IsEmpty)
                {
                    // We should never have a scenario where an item type does not have a parent.
                    TraceUtilities.TraceError($"No IRelation exports exist that provide parent (ContainedBy) items for type {childItem.GetType()}.");
                    return;
                }

                var allParentItems = new List<object>();

                childItem.ContainedByCollection = new AggregateContainedByRelationCollection(allParentItems);

                foreach (IRelation relation in containedByRelations)
                {
                    IEnumerable<IRelatableItem>? relationParentItems = relation.CreateContainedByItems(childItem);

                    if (relationParentItems is not null)
                    {
                        foreach (IRelatableItem parentItem in relationParentItems)
                        {
                            IRelatableItem deduplicateItem = DeduplicateItem(parentItem);
                            allParentItems.Add(deduplicateItem);

                            if (deduplicateItem.TryGetProjectNode(_targetRootNode, parentItem, out IProjectTree? projectNode))
                            {
                                uint itemId = (uint)projectNode.Identity.ToInt32();
                                IVsHierarchyItem hierarchyItem = _hierarchyItemManager.GetHierarchyItem(_projectVsServices.VsHierarchy, itemId);
                                allParentItems.Add(hierarchyItem);
                            }

                            if (deduplicateItem.ContainedByCollection is null)
                            {
                                PopulateAncestors(deduplicateItem);
                            }
                        }
                    }
                }
            }

            IRelatableItem DeduplicateItem(IRelatableItem item)
            {
                object key = GetItemKey(item);

                if (_itemByKey.TryGetValue(key, out IRelatableItem existingItem))
                {
                    return existingItem;
                }

                _itemByKey.Add(key, item);
                return item;
            }
        }

        private static object GetItemKey(IRelatableItem item) => (item.GetType(), item.Identity);
    }
}
