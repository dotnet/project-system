// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Export(typeof(IDependenciesTreeViewProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class GroupedByTargetTreeViewProvider : TreeViewProviderBase
    {
        public const int Order = 1000;

        [ImportingConstructor]
        public GroupedByTargetTreeViewProvider(
            IDependenciesTreeServices treeServices,
            IDependenciesViewModelFactory viewModelFactory,
            IUnconfiguredProjectCommonServices commonServices)
            : base(commonServices.Project)
        {
            TreeServices = treeServices;
            ViewModelFactory = viewModelFactory;
            CommonServices = commonServices;
        }

        private IDependenciesTreeServices TreeServices { get; }
        private IDependenciesViewModelFactory ViewModelFactory { get; }
        private IUnconfiguredProjectCommonServices CommonServices { get; }

        /// <summary>
        /// Builds Dependencies tree for given dependencies snapshot
        /// </summary>
        public override IProjectTree BuildTree(
            IProjectTree dependenciesTree, 
            IDependenciesSnapshot snapshot, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var originalTree = dependenciesTree;
            var currentTopLevelNodes = new List<IProjectTree>();
            Func<IProjectTree, IEnumerable<IProjectTree>, IProjectTree> rememberNewNodes = (rootNode, currentNodes) =>
            {
                if (currentNodes != null)
                {
                    currentTopLevelNodes.AddRange(currentNodes);
                }

                return rootNode;
            };

            if (snapshot.Targets.Where(x => !x.Key.Equals(TargetFramework.Any)).Count() == 1)
            {
                foreach (var target in snapshot.Targets)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return originalTree;
                    }

                    dependenciesTree = BuildSubTrees(
                        dependenciesTree,
                        snapshot.ActiveTarget,
                        target.Value,
                        target.Value.Catalogs,
                        rememberNewNodes);
                }
            }
            else
            {
                foreach (var target in snapshot.Targets)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return originalTree;
                    }

                    if (target.Key.Equals(TargetFramework.Any))
                    {
                        dependenciesTree = BuildSubTrees(dependenciesTree, 
                                                         snapshot.ActiveTarget, 
                                                         target.Value, 
                                                         target.Value.Catalogs, 
                                                         rememberNewNodes);
                    }
                    else
                    {
                        var node = dependenciesTree.FindNodeByCaption(target.Key.FriendlyName);
                        var shouldAddTargetNode = node == null;
                        var targetViewModel = ViewModelFactory.CreateTargetViewModel(target.Value);

                        node = CreateOrUpdateNode(node, 
                                                  targetViewModel, 
                                                  rule:null, 
                                                  isProjectItem:false, 
                                                  additionalFlags: ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp));
                        node = BuildSubTrees(node, snapshot.ActiveTarget, target.Value, target.Value.Catalogs, CleanupOldNodes);

                        if (shouldAddTargetNode)
                        {
                            dependenciesTree = dependenciesTree.Add(node).Parent;
                        }
                        else
                        {
                            dependenciesTree = node.Parent;
                        }

                        currentTopLevelNodes.Add(node);
                    }
                }
            }

            dependenciesTree = CleanupOldNodes(dependenciesTree, currentTopLevelNodes);

            // now update root Dependencies node status
            var rootIcon = ViewModelFactory.GetDependenciesRootIcon(snapshot.HasUnresolvedDependency).ToProjectSystemType();
            return dependenciesTree.SetProperties(icon: rootIcon, expandedIcon: rootIcon);
        }

        public override IProjectTree FindByPath(IProjectTree root, string path)
        {
            if (root == null)
            {
                return null;
            }

            IProjectTree dependenciesNode = null;
            if (root.Flags.Contains(DependencyTreeFlags.DependenciesRootNodeFlags))
            {
                dependenciesNode = root;
            }
            else
            {
                dependenciesNode = root.GetSubTreeNode(DependencyTreeFlags.DependenciesRootNodeFlags);
            }

            if (dependenciesNode == null)
            {
                return null;
            }

            var result = FindByPathInternal(dependenciesNode, path);
            if (result != null)
            {
                return result;
            }

            return FindByPathInternal(dependenciesNode, CommonServices.Project.GetRelativePath(path));
        }

        private IProjectTree FindByPathInternal(IProjectTree root, string path)
        {
            var node = root.FindNodeByPath(path);
            if (node != null)
            {
                return node;
            }

            foreach (var child in root.Children)
            {
                node = child.FindNodeByPath(path);
                if (node != null)
                {
                    return node;
                }

                foreach (var thirdLevelNode in child.Children)
                {
                    node = thirdLevelNode.FindNodeByPath(path);
                    if (node != null)
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Builds all available sub trees under root: target framework or Dependencies node 
        /// when there is only one target.
        /// </summary>
        private IProjectTree BuildSubTrees(
            IProjectTree rootNode,
            ITargetFramework activeTarget,
            ITargetedDependenciesSnapshot targetedSnapshot,
            IProjectCatalogSnapshot catalogs,
            Func<IProjectTree, IEnumerable<IProjectTree>, IProjectTree> syncFunc)
        {
            var currentNodes = new List<IProjectTree>();
            var grouppedByProviderType = new Dictionary<string, List<IDependency>>(StringComparer.OrdinalIgnoreCase);
            foreach(var dependency in targetedSnapshot.TopLevelDependencies)
            {
                if (!dependency.Visible)
                {
                    if (dependency.Flags.Contains(DependencyTreeFlags.ShowEmptyProviderRootNode))
                    {
                        // if provider sends special invisible node with flag ShowEmptyProviderRootNode, we 
                        // need to show provider node even if it does not have any dependencies.
                        grouppedByProviderType.Add(dependency.ProviderType, new List<IDependency>());
                    }

                    continue;
                }

                if (!grouppedByProviderType.TryGetValue(dependency.ProviderType, out List<IDependency> dependencies))
                {
                    dependencies = new List<IDependency>();
                    grouppedByProviderType.Add(dependency.ProviderType, dependencies);
                }

                dependencies.Add(dependency);
            }

            var isActiveTarget = targetedSnapshot.TargetFramework.Equals(activeTarget);
            foreach (var dependencyGroup in grouppedByProviderType)
            {
                var subTreeViewModel = ViewModelFactory.CreateRootViewModel(
                    dependencyGroup.Key, targetedSnapshot.CheckForUnresolvedDependencies(dependencyGroup.Key));
                var subTreeNode = rootNode.FindNodeByCaption(subTreeViewModel.Caption);
                var isNewSubTreeNode = subTreeNode == null;

                var excludedFlags = ProjectTreeFlags.Empty;
                if (targetedSnapshot.TargetFramework.Equals(TargetFramework.Any))
                {
                    excludedFlags = ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp);
                }

                subTreeNode = CreateOrUpdateNode(subTreeNode, subTreeViewModel, rule:null, isProjectItem:false, excludedFlags: excludedFlags);
                subTreeNode = BuildSubTree(subTreeNode, dependencyGroup.Value, catalogs, isActiveTarget, shouldCleanup:!isNewSubTreeNode);
                
                currentNodes.Add(subTreeNode);

                if (isNewSubTreeNode)
                {
                    rootNode = rootNode.Add(subTreeNode).Parent;
                }
                else
                {
                    rootNode = subTreeNode.Parent;
                }
            }

            return syncFunc(rootNode, currentNodes);
        }

        /// <summary>
        /// Builds a sub tree under root: target framework or Dependencies node when there is only one target.
        /// </summary>
        private IProjectTree BuildSubTree(
            IProjectTree rootNode,
            IEnumerable<IDependency> dependencies,
            IProjectCatalogSnapshot catalogs,
            bool isActiveTarget,
            bool shouldCleanup)
        {
            var currentNodes = new List<IProjectTree>();
            foreach (var dependency in dependencies)
            {
                var dependencyNode = rootNode.FindNodeByCaption(dependency.Caption);
                var isNewDependencyNode = dependencyNode == null;

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

                dependencyNode = CreateOrUpdateNode(dependencyNode, dependency, catalogs, isActiveTarget);
                currentNodes.Add(dependencyNode);

                if (isNewDependencyNode)
                {
                    rootNode = rootNode.Add(dependencyNode).Parent;
                }
                else
                {
                    rootNode = dependencyNode.Parent;
                }
            }

            if (shouldCleanup)
            {
                rootNode = CleanupOldNodes(rootNode, currentNodes);
            }

            return rootNode;
        }

        /// <summary>
        /// Removes nodes that don't exist anymore
        /// </summary>
        private IProjectTree CleanupOldNodes(IProjectTree rootNode, IEnumerable<IProjectTree> currentNodes)
        {
            foreach (var nodeToRemove in rootNode.Children.Except(currentNodes))
            {
                rootNode = rootNode.Remove(nodeToRemove);
            }

            return rootNode;
        }

        /// <summary>
        /// Updates or creates new node
        /// </summary>
        private IProjectTree CreateOrUpdateNode(
            IProjectTree node,
            IDependency dependency,
            IProjectCatalogSnapshot catalogs,
            bool isProjectItem,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            IRule rule = null;
            if (dependency.Flags.Contains(DependencyTreeFlags.SupportsRuleProperties))
            {
                rule = TreeServices.GetRule(dependency, catalogs);
            }

            return CreateOrUpdateNode(node, dependency.ToViewModel(), rule, isProjectItem, additionalFlags, excludedFlags);
        }

        /// <summary>
        /// Updates or creates new node
        /// </summary>
        private IProjectTree CreateOrUpdateNode(
            IProjectTree node,
            IDependencyViewModel viewModel,
            IRule rule,
            bool isProjectItem,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            return isProjectItem
                ? CreateOrUpdateProjectItemTreeNode(node, viewModel, rule, additionalFlags, excludedFlags)
                : CreateOrUpdateProjectTreeNode(node, viewModel, rule, additionalFlags, excludedFlags);
        }

        /// <summary>
        /// Updates or creates new IProjectTree node
        /// </summary>
        private IProjectTree CreateOrUpdateProjectTreeNode(
            IProjectTree node,
            IDependencyViewModel viewModel,
            IRule rule,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            if (node == null)
            {
                // For IProjectTree remove ProjectTreeFlags.Common.Reference flag, otherwise CPS would fail to 
                // map this node to graph node and GraphProvider would be never called. 
                // Only IProjectItemTree can have this flag
                var flags = FilterFlags(viewModel.Flags.Except(DependencyTreeFlags.BaseReferenceFlags),
                                        additionalFlags,
                                        excludedFlags);

                node = TreeServices.CreateTree(
                    caption: viewModel.Caption,
                    filePath: viewModel.FilePath,
                    visible: true,
                    browseObjectProperties: rule,
                    flags: flags,
                    icon: viewModel.Icon.ToProjectSystemType(),
                    expandedIcon: viewModel.ExpandedIcon.ToProjectSystemType());
            }
            else
            {
                node = UpdateTreeNode(node, viewModel, rule);
            }

            return node;
        }

        /// <summary>
        /// Updates or creates new IProjectItemTree node
        /// </summary>
        private IProjectTree CreateOrUpdateProjectItemTreeNode(
            IProjectTree node,
            IDependencyViewModel viewModel,
            IRule rule,
            ProjectTreeFlags? additionalFlags = null,
            ProjectTreeFlags? excludedFlags = null)
        {
            if (node == null)
            {
                var flags = FilterFlags(viewModel.Flags, additionalFlags, excludedFlags);

                var itemContext = ProjectPropertiesContext.GetContext(
                    CommonServices.Project,
                    file: viewModel.FilePath,
                    itemType: viewModel.SchemaItemType,
                    itemName: viewModel.FilePath);

                node = TreeServices.CreateTree(
                    caption: viewModel.Caption,
                    itemContext: itemContext,
                    propertySheet: null,
                    visible: true,
                    browseObjectProperties: rule,
                    flags: viewModel.Flags,
                    icon: viewModel.Icon.ToProjectSystemType(),
                    expandedIcon: viewModel.ExpandedIcon.ToProjectSystemType());
            }
            else
            {
                node = UpdateTreeNode(node, viewModel, rule);
            }

            return node;
        }

        private IProjectTree UpdateTreeNode(
            IProjectTree node,
            IDependencyViewModel viewModel,
            IRule rule)
        {
            var updatedNodeParentContext = GetCustomPropertyContext(node.Parent);
            var updatedValues = new ReferencesProjectTreeCustomizablePropertyValues
            {
                Caption = viewModel.Caption,
                Flags = viewModel.Flags,                
                Icon = viewModel.Icon.ToProjectSystemType(),
                ExpandedIcon = viewModel.ExpandedIcon.ToProjectSystemType()
            };

            ApplyProjectTreePropertiesCustomization(updatedNodeParentContext, updatedValues);

            return node.SetProperties(
                    caption: viewModel.Caption,
                    browseObjectProperties: rule,
                    icon: viewModel.Icon.ToProjectSystemType(),
                    expandedIcon: viewModel.ExpandedIcon.ToProjectSystemType());
        }

        private ProjectTreeFlags FilterFlags(
            ProjectTreeFlags flags,
            ProjectTreeFlags? additionalFlags,
            ProjectTreeFlags? excludedFlags)
        {
            if (additionalFlags != null && additionalFlags.HasValue)
            {
                flags = flags.Union(additionalFlags.Value);
            }

            if (excludedFlags != null && excludedFlags.HasValue)
            {
                flags = flags.Except(excludedFlags.Value);
            }

            return flags;
        }
    }
}
