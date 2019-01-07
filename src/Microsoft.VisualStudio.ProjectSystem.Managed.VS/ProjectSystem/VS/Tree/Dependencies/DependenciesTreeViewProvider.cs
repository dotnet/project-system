// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
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

        /// <inheritdoc />
        public async Task<IProjectTree> BuildTreeAsync(
            IProjectTree dependenciesTree,
            IDependenciesSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            IProjectTree originalTree = dependenciesTree;

            var currentTopLevelNodes = new List<IProjectTree>();

            if (snapshot.Targets.Count(x => !x.Key.Equals(TargetFramework.Any)) == 1)
            {
                foreach ((ITargetFramework _, ITargetedDependenciesSnapshot targetedSnapshot) in snapshot.Targets)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return originalTree;
                    }

                    dependenciesTree = await BuildSubTreesAsync(
                        rootNode: dependenciesTree,
                        snapshot.ActiveTarget,
                        targetedSnapshot,
                        RememberNewNodes);
                }
            }
            else
            {
                foreach ((ITargetFramework targetFramework, ITargetedDependenciesSnapshot targetedSnapshot) in snapshot.Targets)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return originalTree;
                    }

                    if (targetFramework.Equals(TargetFramework.Any))
                    {
                        dependenciesTree = await BuildSubTreesAsync(
                            rootNode: dependenciesTree,
                            snapshot.ActiveTarget,
                            targetedSnapshot,
                            RememberNewNodes);
                    }
                    else
                    {
                        IProjectTree node = dependenciesTree.FindChildWithCaption(targetFramework.FriendlyName);
                        bool shouldAddTargetNode = node == null;
                        IDependencyViewModel targetViewModel = _viewModelFactory.CreateTargetViewModel(targetedSnapshot);

                        node = CreateOrUpdateNode(
                            node,
                            targetViewModel,
                            rule: null,
                            isProjectItem: false,
                            additionalFlags: ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp));

                        node = await BuildSubTreesAsync(
                            rootNode: node, 
                            snapshot.ActiveTarget, 
                            targetedSnapshot, 
                            CleanupOldNodes);

                        dependenciesTree = shouldAddTargetNode 
                            ? dependenciesTree.Add(node).Parent 
                            : node.Parent;

                        currentTopLevelNodes.Add(node);
                    }
                }
            }

            dependenciesTree = CleanupOldNodes(dependenciesTree, currentTopLevelNodes);

            // now update root Dependencies node status
            ProjectImageMoniker rootIcon = _viewModelFactory.GetDependenciesRootIcon(snapshot.HasUnresolvedDependency).ToProjectSystemType();
            return dependenciesTree.SetProperties(icon: rootIcon, expandedIcon: rootIcon);

            IProjectTree RememberNewNodes(IProjectTree rootNode, IEnumerable<IProjectTree> currentNodes)
            {
                if (currentNodes != null)
                {
                    currentTopLevelNodes.AddRange(currentNodes);
                }

                return rootNode;
            }
        }

        /// <inheritdoc />
        public IProjectTree FindByPath(IProjectTree root, string path)
        {
            if (root == null)
            {
                return null;
            }

            IProjectTree dependenciesNode = root.Flags.Contains(DependencyTreeFlags.DependenciesRootNodeFlags) 
                ? root 
                : root.GetSubTreeNode(DependencyTreeFlags.DependenciesRootNodeFlags);

            return dependenciesNode?.GetSelfAndDescendentsBreadthFirst()
                .FirstOrDefault((node, p) => string.Equals(node.FilePath, p, StringComparison.OrdinalIgnoreCase), path);
        }

        /// <summary>
        /// Builds all available sub trees under root: target framework or Dependencies node 
        /// when there is only one target.
        /// </summary>
        private async Task<IProjectTree> BuildSubTreesAsync(
            IProjectTree rootNode,
            ITargetFramework activeTarget,
            ITargetedDependenciesSnapshot targetedSnapshot,
            Func<IProjectTree, IEnumerable<IProjectTree>, IProjectTree> syncFunc)
        {
            var groupedByProviderType = new Dictionary<string, List<IDependency>>(StringComparers.DependencyProviderTypes);

            foreach (IDependency dependency in targetedSnapshot.TopLevelDependencies)
            {
                if (!dependency.Visible)
                {
                    if (dependency.Flags.Contains(DependencyTreeFlags.ShowEmptyProviderRootNode))
                    {
                        // if provider sends special invisible node with flag ShowEmptyProviderRootNode, we 
                        // need to show provider node even if it does not have any dependencies.
                        groupedByProviderType.Add(dependency.ProviderType, new List<IDependency>());
                    }

                    continue;
                }

                if (!groupedByProviderType.TryGetValue(dependency.ProviderType, out List<IDependency> dependencies))
                {
                    dependencies = new List<IDependency>();
                    groupedByProviderType.Add(dependency.ProviderType, dependencies);
                }

                dependencies.Add(dependency);
            }

            var currentNodes = new List<IProjectTree>(capacity: groupedByProviderType.Count);

            bool isActiveTarget = targetedSnapshot.TargetFramework.Equals(activeTarget);
            foreach ((string providerType, List<IDependency> dependencies) in groupedByProviderType)
            {
                IDependencyViewModel subTreeViewModel = _viewModelFactory.CreateRootViewModel(
                    providerType, targetedSnapshot.CheckForUnresolvedDependencies(providerType));
                IProjectTree subTreeNode = rootNode.FindChildWithCaption(subTreeViewModel.Caption);
                bool isNewSubTreeNode = subTreeNode == null;

                ProjectTreeFlags excludedFlags = targetedSnapshot.TargetFramework.Equals(TargetFramework.Any) 
                    ? ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp) 
                    : ProjectTreeFlags.Empty;

                subTreeNode = CreateOrUpdateNode(
                    subTreeNode,
                    subTreeViewModel,
                    rule: null,
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
                    ? rootNode.Add(subTreeNode).Parent 
                    : subTreeNode.Parent;
            }

            return syncFunc(rootNode, currentNodes);
        }

        /// <summary>
        /// Builds a sub tree under root: target framework or Dependencies node when there is only one target.
        /// </summary>
        private async Task<IProjectTree> BuildSubTreeAsync(
            IProjectTree rootNode,
            ITargetedDependenciesSnapshot targetedSnapshot,
            List<IDependency> dependencies,
            bool isActiveTarget,
            bool shouldCleanup)
        {
            List<IProjectTree> currentNodes = shouldCleanup 
                ? new List<IProjectTree>(capacity: dependencies.Count) 
                : null;

            foreach (IDependency dependency in dependencies)
            {
                IProjectTree dependencyNode = rootNode.FindChildWithCaption(dependency.Caption);
                bool isNewDependencyNode = dependencyNode == null;

                if (!isNewDependencyNode
                    && dependency.Flags.Contains(DependencyTreeFlags.SupportsHierarchy))
                {
                    if ((dependency.Resolved && dependencyNode.Flags.Contains(DependencyTreeFlags.UnresolvedFlags))
                        || (!dependency.Resolved && dependencyNode.Flags.Contains(DependencyTreeFlags.ResolvedFlags)))
                    {
                        // when transition from unresolved to resolved or vise versa - remove old node
                        // and re-add new  one to allow GraphProvider to recalculate children
                        isNewDependencyNode = true;
                        rootNode = dependencyNode.Remove();
                        dependencyNode = null;
                    }
                }

                dependencyNode = await CreateOrUpdateNodeAsync(dependencyNode, dependency, targetedSnapshot, isActiveTarget);

                currentNodes?.Add(dependencyNode);

                rootNode = isNewDependencyNode
                    ? rootNode.Add(dependencyNode).Parent
                    : dependencyNode.Parent;
            }

            return shouldCleanup 
                ? CleanupOldNodes(rootNode, currentNodes) 
                : rootNode;
        }

        /// <summary>
        /// Removes nodes that don't exist anymore
        /// </summary>
        private static IProjectTree CleanupOldNodes(IProjectTree rootNode, IEnumerable<IProjectTree> currentNodes)
        {
            foreach (IProjectTree nodeToRemove in rootNode.Children.Except(currentNodes))
            {
                rootNode = rootNode.Remove(nodeToRemove);
            }

            return rootNode;
        }

        private async Task<IProjectTree> CreateOrUpdateNodeAsync(
            IProjectTree node,
            IDependency dependency,
            ITargetedDependenciesSnapshot targetedSnapshot,
            bool isProjectItem,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            IRule rule = null;
            if (dependency.Flags.Contains(DependencyTreeFlags.SupportsRuleProperties))
            {
                rule = await _treeServices.GetRuleAsync(dependency, targetedSnapshot.Catalogs);
            }

            return CreateOrUpdateNode(
                node,
                dependency.ToViewModel(targetedSnapshot),
                rule,
                isProjectItem,
                additionalFlags,
                excludedFlags);
        }

        private IProjectTree CreateOrUpdateNode(
            IProjectTree node,
            IDependencyViewModel viewModel,
            IRule rule,
            bool isProjectItem,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            if (node != null)
            {
                return UpdateTreeNode();
            }

            string filePath = viewModel.OriginalModel != null && 
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
                    browseObjectProperties: rule,
                    icon: viewModel.Icon.ToProjectSystemType(),
                    expandedIcon: viewModel.ExpandedIcon.ToProjectSystemType(),
                    visible: true,
                    flags: filteredFlags);
            }

            IProjectTree CreateProjectItemTreeNode()
            {
                var itemContext = ProjectPropertiesContext.GetContext(
                    _commonServices.Project,
                    file: filePath,
                    itemType: viewModel.SchemaItemType,
                    itemName: filePath);

                return _treeServices.CreateTree(
                    caption: viewModel.Caption,
                    itemContext: itemContext,
                    browseObjectProperties: rule,
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
                    ParentNodeFlags = node.Parent?.Flags ?? default
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
                    browseObjectProperties: rule,
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

            public string ItemName => null;

            public string ItemType => null;

            public IImmutableDictionary<string, string> Metadata => null;

            public ProjectTreeFlags ParentNodeFlags { get; set; }

            public bool ExistsOnDisk { get; set; }

            public bool IsFolder => false;

            public bool IsNonFileSystemProjectItem => true;

            public IImmutableDictionary<string, string> ProjectTreeSettings => null;
        }
    }
}
