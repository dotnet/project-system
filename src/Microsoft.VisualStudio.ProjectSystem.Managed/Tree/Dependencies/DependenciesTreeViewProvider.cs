// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// An implementation of <see cref="IDependenciesTreeViewProvider"/> that groups dependencies
    /// by target framework for cross-targeting projects, or without grouping when not cross-targeting.
    /// </summary>
    [Export(typeof(IDependenciesTreeViewProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class DependenciesTreeViewProvider : IDependenciesTreeViewProvider
    {
        private const int Order = 1000;

        private readonly IDependenciesTreeServices _treeServices;
        private readonly IDependenciesViewModelFactory _viewModelFactory;
        private readonly IUnconfiguredProjectCommonServices _commonServices;

        /// <summary><see cref="IProjectTreePropertiesProvider"/> imports that apply to the references tree.</summary>
        [ImportMany(ReferencesProjectTreeCustomizablePropertyValues.ContractName)]
        private readonly OrderPrecedenceImportCollection<IProjectTreePropertiesProvider> _projectTreePropertiesProviders;

        [ImportingConstructor]
        public DependenciesTreeViewProvider(
            IDependenciesTreeServices treeServices,
            IDependenciesViewModelFactory viewModelFactory,
            IUnconfiguredProjectCommonServices commonServices)
        {
            _treeServices = treeServices;
            _viewModelFactory = viewModelFactory;
            _commonServices = commonServices;

            _projectTreePropertiesProviders = new OrderPrecedenceImportCollection<IProjectTreePropertiesProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: commonServices.Project);
        }

        public async Task<IProjectTree> BuildTreeAsync(
            IProjectTree dependenciesTree,
            DependenciesSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            // Keep a reference to the original tree to return in case we are cancelled.
            IProjectTree originalTree = dependenciesTree;

            bool hasSingleTarget = snapshot.DependenciesByTargetFramework.Count(x => !x.Key.Equals(TargetFramework.Any)) == 1;

            var currentTopLevelNodes = new HashSet<IProjectTree>();

            if (hasSingleTarget)
            {
                await BuildSingleTargetTreeAsync();
            }
            else
            {
                await BuildMultiTargetTreeAsync();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return originalTree;
            }

            dependenciesTree = CleanupOldNodes(dependenciesTree, currentTopLevelNodes);

            ProjectImageMoniker rootIcon = _viewModelFactory.GetDependenciesRootIcon(snapshot.HasReachableVisibleUnresolvedDependency).ToProjectSystemType();

            return dependenciesTree.SetProperties(icon: rootIcon, expandedIcon: rootIcon);

            async Task BuildSingleTargetTreeAsync()
            {
                foreach ((ITargetFramework _, TargetedDependenciesSnapshot targetedSnapshot) in snapshot.DependenciesByTargetFramework)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    dependenciesTree = await BuildSubTreesAsync(
                        rootNode: dependenciesTree,
                        snapshot.ActiveTargetFramework,
                        targetedSnapshot,
                        RememberNewNodes);
                }
            }

            async Task BuildMultiTargetTreeAsync()
            {
                foreach ((ITargetFramework targetFramework, TargetedDependenciesSnapshot targetedSnapshot) in snapshot.DependenciesByTargetFramework)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (targetFramework.Equals(TargetFramework.Any))
                    {
                        dependenciesTree = await BuildSubTreesAsync(
                            rootNode: dependenciesTree,
                            snapshot.ActiveTargetFramework,
                            targetedSnapshot,
                            RememberNewNodes);
                    }
                    else
                    {
                        IProjectTree? node = dependenciesTree.FindChildWithCaption(targetFramework.FriendlyName);
                        bool shouldAddTargetNode = node == null;
                        IDependencyViewModel targetViewModel = _viewModelFactory.CreateTargetViewModel(targetedSnapshot);

                        node = CreateOrUpdateNode(
                            node,
                            targetViewModel,
                            browseObjectProperties: null,
                            isProjectItem: false,
                            additionalFlags: ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp));

                        node = await BuildSubTreesAsync(
                            rootNode: node,
                            snapshot.ActiveTargetFramework,
                            targetedSnapshot,
                            CleanupOldNodes);

                        dependenciesTree = shouldAddTargetNode
                            ? dependenciesTree.Add(node).Parent!
                            : node.Parent!;

                        Assumes.NotNull(dependenciesTree);

                        currentTopLevelNodes.Add(node);
                    }
                }
            }

            IProjectTree RememberNewNodes(IProjectTree rootNode, IEnumerable<IProjectTree> currentNodes)
            {
                if (currentNodes != null)
                {
                    currentTopLevelNodes.AddRange(currentNodes);
                }

                return rootNode;
            }
        }

        public IProjectTree? FindByPath(IProjectTree? root, string path)
        {
            if (root == null)
            {
                return null;
            }

            IProjectTree? dependenciesNode = root.Flags.Contains(DependencyTreeFlags.DependenciesRootNodeFlags)
                ? root
                : root.GetSubTreeNode(DependencyTreeFlags.DependenciesRootNodeFlags);

            return dependenciesNode?.GetSelfAndDescendentsBreadthFirst()
                .FirstOrDefault((node, p) => string.Equals(node.FilePath, p, StringComparisons.Paths), path);
        }

        /// <summary>
        /// Builds all available sub trees under root: target framework or Dependencies node
        /// when there is only one target.
        /// </summary>
        private async Task<IProjectTree> BuildSubTreesAsync(
            IProjectTree rootNode,
            ITargetFramework activeTarget,
            TargetedDependenciesSnapshot targetedSnapshot,
            Func<IProjectTree, HashSet<IProjectTree>, IProjectTree> syncFunc)
        {
            var groupedByProviderType = new Dictionary<string, List<IDependency>>(StringComparers.DependencyProviderTypes);

            foreach (IDependency dependency in targetedSnapshot.TopLevelDependencies)
            {
                if (!dependency.Visible)
                {
                    // If a dependency is not visible we will still register a top-level group if it
                    // has the ShowEmptyProviderRootNode flag.
                    if (!dependency.Flags.Contains(DependencyTreeFlags.ShowEmptyProviderRootNode))
                    {
                        // No such flag, so skip it completely.
                        continue;
                    }
                }

                if (!groupedByProviderType.TryGetValue(dependency.ProviderType, out List<IDependency> dependencies))
                {
                    dependencies = new List<IDependency>();
                    groupedByProviderType.Add(dependency.ProviderType, dependencies);
                }

                // Only add visible dependencies. See note above.
                if (dependency.Visible)
                {
                    dependencies.Add(dependency);
                }
            }

            var currentNodes = new HashSet<IProjectTree>(capacity: groupedByProviderType.Count);

            bool isActiveTarget = targetedSnapshot.TargetFramework.Equals(activeTarget);
            foreach ((string providerType, List<IDependency> dependencies) in groupedByProviderType)
            {
                IDependencyViewModel? subTreeViewModel = _viewModelFactory.CreateRootViewModel(
                    providerType,
                    targetedSnapshot.CheckForUnresolvedDependencies(providerType));

                if (subTreeViewModel == null)
                {
                    // In theory this should never happen, as it means we have a dependency model of a type
                    // that no provider claims. https://github.com/dotnet/project-system/issues/3653
                    continue;
                }

                IProjectTree? subTreeNode = rootNode.FindChildWithCaption(subTreeViewModel.Caption);
                bool isNewSubTreeNode = subTreeNode == null;

                ProjectTreeFlags excludedFlags = targetedSnapshot.TargetFramework.Equals(TargetFramework.Any)
                    ? ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp)
                    : ProjectTreeFlags.Empty;

                subTreeNode = CreateOrUpdateNode(
                    subTreeNode,
                    subTreeViewModel,
                    browseObjectProperties: null,
                    isProjectItem: false,
                    excludedFlags: excludedFlags);

                subTreeNode = await BuildSubTreeAsync(
                    subTreeNode,
                    targetedSnapshot,
                    dependencies,
                    isActiveTarget,
                    shouldCleanup: !isNewSubTreeNode);

                currentNodes.Add(subTreeNode);

                rootNode = isNewSubTreeNode
                    ? rootNode.Add(subTreeNode).Parent!
                    : subTreeNode.Parent!;

                Assumes.NotNull(rootNode);
            }

            return syncFunc(rootNode, currentNodes);
        }

        /// <summary>
        /// Builds a sub tree under root: target framework or Dependencies node when there is only one target.
        /// </summary>
        private async Task<IProjectTree> BuildSubTreeAsync(
            IProjectTree rootNode,
            TargetedDependenciesSnapshot targetedSnapshot,
            List<IDependency> dependencies,
            bool isActiveTarget,
            bool shouldCleanup)
        {
            HashSet<IProjectTree>? currentNodes = shouldCleanup
                ? new HashSet<IProjectTree>(capacity: dependencies.Count)
                : null;

            foreach (IDependency dependency in dependencies)
            {
                IProjectTree? dependencyNode = rootNode.FindChildWithCaption(dependency.Caption);
                bool isNewDependencyNode = dependencyNode == null;

                if (dependencyNode != null
                    && dependency.Flags.Contains(DependencyTreeFlags.SupportsHierarchy))
                {
                    if ((dependency.Resolved && dependencyNode.Flags.Contains(DependencyTreeFlags.Unresolved))
                        || (!dependency.Resolved && dependencyNode.Flags.Contains(DependencyTreeFlags.Resolved)))
                    {
                        // when transition from unresolved to resolved or vise versa - remove old node
                        // and re-add new  one to allow GraphProvider to recalculate children
                        isNewDependencyNode = true;
                        rootNode = dependencyNode.Remove();
                        dependencyNode = null;
                    }
                }

                // NOTE this project system supports multiple implicit configuration dimensions (such as target framework)
                // which is a concept not modelled by DTE/VSLangProj. In order to produce a sensible view of the project
                // via automation, we expose only the active target framework at any given time.
                //
                // This is achieved by using IProjectItemTree for active target framework items, and IProjectTree for inactive
                // target frameworks. CPS only creates automation objects for items with "Reference" flag if they implement
                // IProjectItemTree. See SimpleItemNode.Initialize (in CPS) for details.

                dependencyNode = await CreateOrUpdateNodeAsync(
                    dependencyNode,
                    dependency,
                    targetedSnapshot,
                    isProjectItem: isActiveTarget);

                currentNodes?.Add(dependencyNode);

                IProjectTree? parent = isNewDependencyNode
                    ? rootNode.Add(dependencyNode).Parent
                    : dependencyNode.Parent;

                Assumes.NotNull(parent);

                rootNode = parent!;
            }

            return currentNodes != null // shouldCleanup
                ? CleanupOldNodes(rootNode, currentNodes)
                : rootNode;
        }

        /// <summary>
        /// Removes nodes that don't exist anymore
        /// </summary>
        private static IProjectTree CleanupOldNodes(IProjectTree rootNode, HashSet<IProjectTree> currentNodes)
        {
            foreach (IProjectTree child in rootNode.Children)
            {
                if (!currentNodes.Contains(child))
                {
                    rootNode = rootNode.Remove(child);
                }
            }

            return rootNode;
        }

        private async Task<IProjectTree> CreateOrUpdateNodeAsync(
            IProjectTree? node,
            IDependency dependency,
            TargetedDependenciesSnapshot targetedSnapshot,
            bool isProjectItem,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            IRule? browseObjectProperties = dependency.Flags.Contains(DependencyTreeFlags.SupportsRuleProperties)
                ? await _treeServices.GetBrowseObjectRuleAsync(dependency, targetedSnapshot.Catalogs)
                : null;

            return CreateOrUpdateNode(
                node,
                dependency.ToViewModel(targetedSnapshot),
                browseObjectProperties,
                isProjectItem,
                additionalFlags,
                excludedFlags);
        }

        private IProjectTree CreateOrUpdateNode(
            IProjectTree? node,
            IDependencyViewModel viewModel,
            IRule? browseObjectProperties,
            bool isProjectItem,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            if (node != null)
            {
                return UpdateTreeNode();
            }

            string? filePath = viewModel.OriginalModel != null &&
                               viewModel.OriginalModel.TopLevel &&
                               viewModel.OriginalModel.Resolved
                ? viewModel.OriginalModel.GetTopLevelId()
                : viewModel.FilePath;

            ProjectTreeFlags filteredFlags = FilterFlags(viewModel.Flags);

            return isProjectItem
                ? CreateProjectItemTreeNode()
                : CreateProjectTreeNode();

            IProjectTree CreateProjectTreeNode()
            {
                // For IProjectTree remove ProjectTreeFlags.Common.Reference flag, otherwise CPS would fail to
                // map this node to graph node and GraphProvider would be never called.
                // Only IProjectItemTree can have this flag
                filteredFlags = filteredFlags.Except(DependencyTreeFlags.BaseReferenceFlags);

                return _treeServices.CreateTree(
                    caption: viewModel.Caption,
                    filePath,
                    browseObjectProperties: browseObjectProperties,
                    icon: viewModel.Icon.ToProjectSystemType(),
                    expandedIcon: viewModel.ExpandedIcon.ToProjectSystemType(),
                    visible: true,
                    flags: filteredFlags);
            }

            IProjectTree CreateProjectItemTreeNode()
            {
                Assumes.NotNull(filePath);

                var itemContext = ProjectPropertiesContext.GetContext(
                    _commonServices.Project,
                    file: filePath,
                    itemType: viewModel.SchemaItemType,
                    itemName: filePath);

                return _treeServices.CreateTree(
                    caption: viewModel.Caption,
                    itemContext: itemContext,
                    browseObjectProperties: browseObjectProperties,
                    icon: viewModel.Icon.ToProjectSystemType(),
                    expandedIcon: viewModel.ExpandedIcon.ToProjectSystemType(),
                    visible: true,
                    flags: filteredFlags);
            }

            IProjectTree UpdateTreeNode()
            {
                var updatedNodeParentContext = new ProjectTreeCustomizablePropertyContext
                {
                    ExistsOnDisk = false,
                    ParentNodeFlags = node!.Parent?.Flags ?? default
                };

                var updatedValues = new ReferencesProjectTreeCustomizablePropertyValues
                {
                    Caption = viewModel.Caption,
                    Flags = viewModel.Flags,
                    Icon = viewModel.Icon.ToProjectSystemType(),
                    ExpandedIcon = viewModel.ExpandedIcon.ToProjectSystemType()
                };

                foreach (Lazy<IProjectTreePropertiesProvider, IOrderPrecedenceMetadataView> provider in _projectTreePropertiesProviders)
                {
                    provider.Value.CalculatePropertyValues(updatedNodeParentContext, updatedValues);
                }

                return node.SetProperties(
                    caption: updatedValues.Caption,
                    browseObjectProperties: browseObjectProperties,
                    icon: updatedValues.Icon,
                    expandedIcon: updatedValues.ExpandedIcon,
                    flags: updatedValues.Flags);
            }

            ProjectTreeFlags FilterFlags(ProjectTreeFlags flags)
            {
                if (additionalFlags.HasValue)
                {
                    flags = flags.Union(additionalFlags.Value);
                }

                if (excludedFlags.HasValue)
                {
                    flags = flags.Except(excludedFlags.Value);
                }

                return flags;
            }
        }

        /// <summary>
        /// A private implementation of <see cref="IProjectTreeCustomizablePropertyContext"/> used when updating
        /// dependencies nodes.
        /// </summary>
        private sealed class ProjectTreeCustomizablePropertyContext : IProjectTreeCustomizablePropertyContext
        {
            // NOTE properties with hard-coded results are currently not set, and
            //      we avoid creating backing fields for them to keep the size of
            //      this class down. They can be changed as needed in future.

            public string? ItemName => null;

            public string? ItemType => null;

            public IImmutableDictionary<string, string>? Metadata => null;

            public ProjectTreeFlags ParentNodeFlags { get; set; }

            public bool ExistsOnDisk { get; set; }

            public bool IsFolder => false;

            public bool IsNonFileSystemProjectItem => true;

            public IImmutableDictionary<string, string>? ProjectTreeSettings => null;
        }
    }
}
