// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides the special "Dependencies" folder to project trees.
    /// </summary>
    [Export(ExportContractNames.ProjectTreeProviders.PhysicalViewRootGraft, typeof(IProjectTreeProvider))]
    [Export(typeof(IDependenciesGraphProjectContext))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class DependenciesProjectTreeProvider : ProjectTreeProviderBase, 
                                                     IProjectTreeProvider, 
                                                     IDependenciesGraphProjectContext
    {
        public const string GenericDependencyNodeFlag = "DependencyNode";
        public const string DependenciesRootNodeFlag = "DependenciesRootNode";
        public const string DependenciesProviderFlag = "DependenciesProvider";
        public const string AssembliesSubTreeRootNodeFlag = "AssembliesSubTreeRootNode";

        /// <summary>
        /// The set of flags common to all Reference nodes.
        /// </summary>
        private static readonly ProjectTreeFlags BaseReferenceFlags 
                = ProjectTreeFlags.Create(ProjectTreeFlags.Common.Reference);

        /// <summary>
        /// The set of flags to assign to unresolvable Reference nodes.
        /// </summary>
        private static readonly ProjectTreeFlags UnresolvedReferenceFlags 
                = BaseReferenceFlags.Add(ProjectTreeFlags.Common.BrokenReference);

        /// <summary>
        /// The set of flags to assign to resolved Reference nodes.
        /// </summary>
        private static readonly ProjectTreeFlags ResolvedReferenceFlags 
                = BaseReferenceFlags.Add(ProjectTreeFlags.Common.ResolvedReference);

        /// <summary>
        /// A flag to add to identify the loading tree.
        /// TODO Figure what to do with "Loading.." message and this flag - should Dependencies caption
        /// also say Loading when any of it's providers is loading or each individual provider should indicate 
        /// it's progress. 
        /// </summary>
        private static readonly string AssembliesLoadingTreeFlag = "AssembliesLoadingTree";

        // TODO: Consider moving this to an [AppliesTo] attribute and
        // removing the GetIsApplicableAsync method or always returning true from it.
        /// <summary>
        /// The AppliesTo expression that must be satisfied for this subtree provider to be applicable.
        /// </summary>
        private static readonly string ApplicabilityTest 
                = string.Format(CultureInfo.InvariantCulture, "{0} & ({1} | {2} | {3} | {4} | {5})",
                                ProjectCapabilities.ReferencesFolder,
                                ProjectCapabilities.AssemblyReferences,
                                ProjectCapabilities.ComReferences,
                                ProjectCapabilities.ProjectReferences,
                                ProjectCapabilities.SdkReferences,
                                ProjectCapabilities.WinRTReferences);

        /// <summary>
        /// The set of flags assigned to shared import reference nodes.
        /// </summary>
        private static readonly ProjectTreeFlags SharedImportReferenceFlags 
                    = ProjectTreeFlags.Create(ProjectTreeFlags.Common.SharedProjectImportReference);

        /// <summary>
        /// The set of rule names that represent unresolved references.
        /// </summary>
        private static readonly ImmutableHashSet<string> UnresolvedReferenceRuleNames
                = Empty.OrdinalIgnoreCaseStringSet
                       .Add(AssemblyReference.SchemaName)
                       .Add(ProjectReference.SchemaName)
                       .Add(ComReference.SchemaName)
                       .Add(SdkReference.SchemaName);

        /// <summary>
        /// The set of rule names that represent resolved references.
        /// </summary>
        private static readonly ImmutableHashSet<string> ResolvedReferenceRuleNames 
                    = Empty.OrdinalIgnoreCaseStringSet
                           .Add(ResolvedProjectReference.SchemaName)
                           .Add(ResolvedAssemblyReference.SchemaName)
                           .Add(ResolvedCOMReference.SchemaName)
                           .Add(ResolvedSdkReference.SchemaName);

        /// <summary>
        /// A value indicating whether the initial population of the tree with actual references has completed.
        /// </summary>
        private bool _initialFillCompleted;

        /// <summary>
        /// The value to dispose to terminate the link providing evaluation snapshots.
        /// </summary>
        private IDisposable _projectSubscriptionLink;

        /// <summary>
        /// The value to dispose to terminate the SyncLinkTo subscription.
        /// </summary>
        private IDisposable _projectSyncLink;

        /// <summary>
        /// Keeps latest updated snapshot of all rules schema catalogs
        /// </summary>
        private IImmutableDictionary<string, IPropertyPagesCatalog> NamedCatalogs { get; set; }

        /// <summary>
        /// Gets the collection of <see cref="IProjectTreePropertiesProvider"/> imports 
        /// that apply to the references tree.
        /// </summary>
        [ImportMany(ReferencesProjectTreeCustomizablePropertyValues.ContractName)]
        private OrderPrecedenceImportCollection<IProjectTreePropertiesProvider> ProjectTreePropertiesProviders { get; set; }

        /// <summary>
        /// Provides a way to extend Dependencies node by consuming sub tree providers that represent
        /// a particular dependency type and are responsible for Dependencies\[TypeNode] and it's contents.
        /// </summary>
        [ImportMany]
        private OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider> SubTreeProviders { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependenciesProjectTreeProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        public DependenciesProjectTreeProvider(IProjectThreadingService threadingService, 
                                               UnconfiguredProject unconfiguredProject)
            : base(threadingService, unconfiguredProject)
        {
            ProjectTreePropertiesProviders = new OrderPrecedenceImportCollection<IProjectTreePropertiesProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: unconfiguredProject);

            SubTreeProviders = new OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider>(
                                    ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                                    projectCapabilityCheckProvider: unconfiguredProject);
        }

        /// <summary>
        /// See <see cref="IProjectTreeProvider"/>
        /// </summary>
        /// <remarks>
        /// This stub defined for code contracts.
        /// </remarks>
        IReceivableSourceBlock<IProjectVersionedValue<IProjectTreeSnapshot>> IProjectTreeProvider.Tree
        {
            get { return Tree; }
        }

        /// <summary>
        /// Gets a value indicating whether a given set of nodes can be copied or moved underneath some given node.
        /// </summary>
        /// <param name="nodes">The set of nodes the user wants to copy or move.</param>
        /// <param name="receiver">
        /// The target node where <paramref name="nodes"/> should be copied or moved to.
        /// May be <c>null</c> to determine whether a given set of nodes could allowably be copied anywhere (not 
        /// necessarily everywhere).
        /// </param>
        /// <param name="deleteOriginal"><c>true</c> for a move operation; <c>false</c> for a copy operation.</param>
        /// <returns><c>true</c> if such a move/copy operation would be allowable; <c>false</c> otherwise.</returns>
        public override bool CanCopy(IImmutableSet<IProjectTree> nodes, 
                                     IProjectTree receiver, 
                                     bool deleteOriginal = false)
        {
            return true;
        }

        /// <summary>
        /// Gets a value indicating whether deleting a given set of items from the project, and optionally from disk,
        /// would be allowed.
        /// </summary>
        /// <param name="nodes">The nodes that should be deleted.</param>
        /// <param name="deleteOptions">A value indicating whether the items should be deleted from disk as well as 
        /// from the project file.
        /// </param>
        public override bool CanRemove(IImmutableSet<IProjectTree> nodes, 
                                       DeleteOptions deleteOptions = DeleteOptions.None)
        {
            if (deleteOptions.HasFlag(DeleteOptions.DeleteFromStorage))
            {
                return false;
            }

            return nodes.All(node => node is IProjectItemTree 
                                     || node.Flags.Contains(ProjectTreeFlags.Common.SharedProjectImportReference));
        }

        /// <summary>
        /// Deletes items from the project, and optionally from disk.
        /// </summary>
        /// <param name="nodes">The nodes that should be deleted.</param>
        /// <param name="deleteOptions">A value indicating whether the items should be deleted from disk as well as 
        /// from the project file.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="IProjectTreeProvider.CanRemove"/> 
        /// would return <c>false</c> for this operation.</exception>
        public override async Task RemoveAsync(IImmutableSet<IProjectTree> nodes, 
                                               DeleteOptions deleteOptions = DeleteOptions.None)
        {
            if (deleteOptions.HasFlag(DeleteOptions.DeleteFromStorage))
            {
                throw new NotSupportedException();
            }

            // Get the list of shared import nodes.
            IEnumerable<IProjectTree> sharedImportNodes = nodes.Where(node => 
                    node.Flags.Contains(ProjectTreeFlags.Common.SharedProjectImportReference));

            // Get the list of normal reference Item Nodes (this excludes any shared import nodes).
            IEnumerable<IProjectItemTree> referenceItemNodes = nodes.Except(sharedImportNodes)
                                                                    .Cast<IProjectItemTree>();

            using (var access = await ProjectLockService.WriteLockAsync())
            {
                var project = await access.GetProjectAsync(ActiveConfiguredProject).ConfigureAwait(false);

                // Handle the removal of normal reference Item Nodes (this excludes any shared import nodes).
                foreach (var nodeItemContext in referenceItemNodes.Select(n => n.GetProjectPropertiesContext()))
                {
                    var unresolvedReferenceItem = project.GetItemsByEvaluatedInclude(nodeItemContext.ItemName)
                        .FirstOrDefault(item => string.Equals(item.ItemType, 
                                                              nodeItemContext.ItemType, 
                                                              StringComparison.OrdinalIgnoreCase));

                    Report.IfNot(unresolvedReferenceItem != null, "Cannot find reference to remove.");
                    if (unresolvedReferenceItem != null)
                    {
                        await access.CheckoutAsync(unresolvedReferenceItem.Xml.ContainingProject.FullPath)
                                    .ConfigureAwait(false);
                        project.RemoveItem(unresolvedReferenceItem);
                    }
                }

                // Handle the removal of shared import nodes.
                var projectXml = await access.GetProjectXmlAsync(UnconfiguredProject.FullPath)
                                             .ConfigureAwait(false);
                foreach (var sharedImportNode in sharedImportNodes)
                {
                    // Find the import that is included in the evaluation of the specified ConfiguredProject that
                    // imports the project file whose full path matches the specified one.
                    var matchingImports = from import in project.Imports
                                          where import.ImportingElement.ContainingProject == projectXml
                                          where PathHelper.IsSamePath(import.ImportedProject.FullPath, 
                                                                      sharedImportNode.FilePath)
                                          select import;
                    foreach (var importToRemove in matchingImports)
                    {
                        var importingElementToRemove = importToRemove.ImportingElement;
                        Report.IfNot(importingElementToRemove != null, 
                                     "Cannot find shared project reference to remove.");
                        if (importingElementToRemove != null)
                        {
                            await access.CheckoutAsync(importingElementToRemove.ContainingProject.FullPath)
                                        .ConfigureAwait(false);
                            importingElementToRemove.Parent.RemoveChild(importingElementToRemove);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds dependencies child nodes by their path. We need to override it since
        /// we need to find children under either:
        ///     - our dependencies root node.
        ///     - dependency sub tree nodes
        ///     - dependency sub tree top level nodes
        /// (deeper levels will be graph nodes with additional info, not direct dependencies
        /// specified in the project file or project.json)
        /// </summary>
        public override IProjectTree FindByPath(IProjectTree root, string path)
        {
            var dependenciesNode = GetSubTreeRootNode(root, ProjectTreeFlags.Create(DependenciesRootNodeFlag));
            if (dependenciesNode == null)
            {
                return null;
            }

            var node = dependenciesNode.FindNodeByPath(path);
            if (node == null)
            {
                foreach (var child in dependenciesNode.Children)
                {
                    node = child.FindNodeByPath(path);

                    if (node != null)
                    {
                        break;
                    }
                }
            }

            return node;
        }

        /// <summary>
        /// This is still needed for graph nodes search
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override string GetPath(IProjectTree node)
        {
            return node.FilePath;
        }

        /// <summary>
        /// Generates the original references directory tree.
        /// </summary>
        protected override void Initialize()
        {
            using (UnconfiguredProjectAsynchronousTasksService.LoadedProject())
            {
                base.Initialize();

                // this.IsApplicable may take a project lock, so we can't do it inline with this method
                // which is holding a private lock.  It turns out that doing it asynchronously isn't a problem anyway,
                // so long as we guard against races with the Dispose method.
                UnconfiguredProjectAsynchronousTasksService.LoadedProjectAsync(
                    async delegate
                    {
                        await TaskScheduler.Default.SwitchTo(alwaysYield: true);
                        UnconfiguredProjectAsynchronousTasksService
                            .UnloadCancellationToken.ThrowIfCancellationRequested();

                        if (!await GetIsApplicableAsync().ConfigureAwait(false))
                        {
                            return;
                        }

                        lock (SyncObject)
                        {
                            foreach (var provider in SubTreeProviders)
                            {
                                provider.Value.DependenciesChanged += OnDependenciesChanged;
                            }

                            Verify.NotDisposed(this);
                            var nowait = SubmitTreeUpdateAsync(
                                (treeSnapshot, configuredProjectExports, cancellationToken) =>
                                    {
                                        var dependenciesNode = CreateDependenciesFolder(null);
                                        dependenciesNode = CreateLoadingAssembliesNoder(dependenciesNode).Parent;
                                        dependenciesNode = CreateOrUpdateSubTreeProviderNodes(dependenciesNode, 
                                                                                              cancellationToken);

                                        return Task.FromResult(new TreeUpdateResult(dependenciesNode, true));
                                    });

                            var intermediateBlock = new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>();
                            _projectSubscriptionLink = ProjectSubscriptionService.JointRuleSource.SourceBlock.LinkTo(
                                intermediateBlock,
                                ruleNames: UnresolvedReferenceRuleNames.Union(ResolvedReferenceRuleNames),
                                suppressVersionOnlyUpdates: false);

                            var actionSubscriptionServiceChanged = new Action<IProjectVersionedValue<
                                            Tuple<IProjectSubscriptionUpdate,
                                                  IProjectCatalogSnapshot,
                                                  IProjectSharedFoldersSnapshot>>>(ProjectSubscriptionService_Changed);

                            var actionBlock = new ActionBlock<IProjectVersionedValue<
                                                                Tuple<IProjectSubscriptionUpdate,
                                                                      IProjectCatalogSnapshot,
                                                                      IProjectSharedFoldersSnapshot>>>(
                                    actionSubscriptionServiceChanged,
                                    new ExecutionDataflowBlockOptions() { NameFormat = "ReferencesSubtree Input: {1}" });

                            _projectSyncLink = ProjectDataSources.SyncLinkTo(
                                intermediateBlock.SyncLinkOptions(),
                                ProjectSubscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                                ProjectSubscriptionService.SharedFoldersSource.SourceBlock.SyncLinkOptions(),
                                actionBlock);
                        }

                    },
                    registerFaultHandler: true);
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _projectSubscriptionLink.DisposeIfNotNull();
                _projectSyncLink.DisposeIfNotNull();

                foreach (var provider in SubTreeProviders)
                {
                    provider.Value.DependenciesChanged -= OnDependenciesChanged;
                }

                ProjectContextUnloaded?.Invoke(this, new ProjectContextEventArgs(this));
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Creates a new instance of the configured project exports class.
        /// </summary>
        protected override ConfiguredProjectExports GetActiveConfiguredProjectExports(
                                ConfiguredProject newActiveConfiguredProject)
        {
            Requires.NotNull(newActiveConfiguredProject, nameof(newActiveConfiguredProject));

            return base.GetActiveConfiguredProjectExports<MyConfiguredProjectExports>(newActiveConfiguredProject);
        }

        /// <summary>
        /// Gets a value indicating whether the References folder should appear in this project.
        /// </summary>
        protected Task<bool> GetIsApplicableAsync()
        {
            // TODO Figure out capabilities that support Dependencies node 
            // await this.InitialActiveConfiguredProjectAvailable;
            // return this.UnconfiguredProject.Capabilities.AppliesTo(ApplicabilityTest);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Finds the resolved reference item for a given unresolved reference.
        /// </summary>
        /// <param name="allResolvedReferences">The collection of resolved references to search.</param>
        /// <param name="unresolvedItemType">The unresolved reference item type.</param>
        /// <param name="unresolvedItemSpec">The unresolved reference item name.</param>
        /// <returns>The key is item name and the value is the metadata dictionary.</returns>
        private static KeyValuePair<string, IImmutableDictionary<string, string>>? GetResolvedReference(
                        IProjectRuleSnapshot[] allResolvedReferences, 
                        string unresolvedItemType, 
                        string unresolvedItemSpec)
        {
            Contract.Requires(allResolvedReferences != null);
            Contract.Requires(Contract.ForAll(0, allResolvedReferences.Length, i => allResolvedReferences[i] != null));
            Contract.Requires(!string.IsNullOrEmpty(unresolvedItemType));
            Contract.Requires(!string.IsNullOrEmpty(unresolvedItemSpec));

            foreach (var resolvedReferences in allResolvedReferences)
            {
                foreach (var referencePath in resolvedReferences.Items)
                {
                    string originalItemSpec;
                    if (referencePath.Value.TryGetValue(ResolvedAssemblyReference.OriginalItemSpecProperty, 
                                                        out originalItemSpec) 
                        && !string.IsNullOrEmpty(originalItemSpec))
                    {
                        if (string.Equals(originalItemSpec, unresolvedItemSpec, StringComparison.OrdinalIgnoreCase))
                        {
                            return referencePath;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds an existing tree node that represents a given reference item.
        /// </summary>
        /// <param name="tree">The reference folder to search.</param>
        /// <param name="itemType">The item type of the unresolved reference.</param>
        /// <param name="itemName">The item name of the unresolved reference.</param>
        /// <returns>The matching tree node, or <c>null</c> if none was found.</returns>
        private static IProjectItemTree FindReferenceNode(IProjectTree tree, string itemType, string itemName)
        {
            Contract.Requires(tree != null);
            Contract.Requires(!string.IsNullOrEmpty(itemType));
            Contract.Requires(!string.IsNullOrEmpty(itemName));

            return tree.Children.OfType<IProjectItemTree>()
                       .FirstOrDefault(child => string.Equals(itemType, child.Item.ItemType, 
                                                           StringComparison.OrdinalIgnoreCase)
                                                && string.Equals(itemName, child.Item.ItemName, 
                                                              StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds a tree node by it's flags. If there many nodes that sattisfy flags, returns first.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private static IProjectTree GetSubTreeRootNode(IProjectTree parentNode, ProjectTreeFlags flags)
        {
            foreach (IProjectTree child in parentNode.Children)
            {
                if (child.Flags.Contains(flags))
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates the loading References folder node.
        /// </summary>
        /// <returns>a new "References (loading...)" tree node.</returns>
        private IProjectTree CreateDependenciesFolder(IProjectTree oldNode)
        {
            if (oldNode == null)
            {
                var values = new ReferencesProjectTreeCustomizablePropertyValues
                {
                    Caption = Resources.DependenciesNodeName,
                    Icon = KnownMonikers.Reference.ToProjectSystemType(),
                    ExpandedIcon = KnownMonikers.Reference.ToProjectSystemType(),
                    Flags = ProjectTreeFlags.Create(
                            ProjectTreeFlags.Common.BubbleUp
                            | ProjectTreeFlags.Common.ReferencesFolder
                            | ProjectTreeFlags.Common.VirtualFolder)
                };

                ApplyProjectTreePropertiesCustomization(null, values);

                return NewTree(
                         values.Caption,
                         icon: values.Icon,
                         expandedIcon: values.ExpandedIcon,
                         flags: values.Flags);
            }
            else
            {
                return oldNode;
            }
        }

        /// <summary>
        /// Creates or updates the Assemblies sub tree root node.
        /// </summary>
        /// <returns>A new Assemblies tree node.</returns>
        private IProjectTree CreateAssembliesNode(IProjectTree dependenciesNode)
        {
            Requires.NotNull(dependenciesNode, nameof(dependenciesNode));

            // assemblies node already must be created by this time with Loading.. postfix
            var assembliesNode = GetSubTreeRootNode(dependenciesNode, 
                                                    ProjectTreeFlags.Create(AssembliesSubTreeRootNodeFlag));

            Requires.NotNull(assembliesNode, nameof(assembliesNode));

            var values = new ReferencesProjectTreeCustomizablePropertyValues
            {
                Caption = assembliesNode.Caption,
                Icon = assembliesNode.Icon,
                ExpandedIcon = assembliesNode.ExpandedIcon,
                Flags = assembliesNode.Flags
            };

            if (assembliesNode.Flags.Contains(AssembliesLoadingTreeFlag))
            {
                values.Caption = Resources.AssembliesNodeName;
                values.Flags = values.Flags.Remove(AssembliesLoadingTreeFlag);
            }

            ApplyProjectTreePropertiesCustomization(null, values);

            assembliesNode = assembliesNode.SetProperties(
                caption: values.Caption,
                icon: values.Icon,
                expandedIcon: values.ExpandedIcon,
                flags: values.Flags);

            return assembliesNode;
        }

        /// <summary>
        /// Creates the loading Assemblies sub tree node.
        /// </summary>
        /// <returns>a new "Assemblies (loading...)" tree node.</returns>
        private IProjectTree CreateLoadingAssembliesNoder(IProjectTree dependenciesNode)
        {
            Requires.NotNull(dependenciesNode, nameof(dependenciesNode));

            var values = new ReferencesProjectTreeCustomizablePropertyValues
            {
                Caption = Resources.AssembliesNodeName + Resources.DependenciesLoadingPostfix,
                Icon = KnownMonikers.Reference.ToProjectSystemType(),
                ExpandedIcon = KnownMonikers.Reference.ToProjectSystemType(),
                Flags = ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp
                                                | ProjectTreeFlags.Common.ReferencesFolder
                                                | ProjectTreeFlags.Common.VirtualFolder)
                                        .Add(AssembliesLoadingTreeFlag)
                                        .Add(AssembliesSubTreeRootNodeFlag)
            };

            ApplyProjectTreePropertiesCustomization(null, values);

            var assembliesNode = NewTree(
                     values.Caption,
                     icon: values.Icon,
                     expandedIcon: values.ExpandedIcon,
                     flags: values.Flags);

            return dependenciesNode.Add(assembliesNode);
        }

        /// <summary>
        /// Creates or updates nodes for all known IProjectDependenciesSubTreeProvider implementations.
        /// </summary>
        /// <param name="dependenciesNode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>IProjectTree for root Dependencies node</returns>
        private IProjectTree CreateOrUpdateSubTreeProviderNodes(IProjectTree dependenciesNode,
                                                                CancellationToken cancellationToken)
        {
            Requires.NotNull(dependenciesNode, nameof(dependenciesNode));

            foreach (var subTreeProvider in SubTreeProviders)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var providerRootTreeNode = GetSubTreeRootNode(dependenciesNode, 
                                                              subTreeProvider.Value.RootNode.Flags);
                // since this method only creates dependencies providers sub tree nodes
                // at initialization time, changes and catalogs could be null.
                dependenciesNode = CreateOrUpdateSubTreeProviderNode(dependenciesNode,
                                                                     subTreeProvider.Value,
                                                                     changes: null,                                                                    
                                                                     catalogs: null,
                                                                     cancellationToken: cancellationToken);
            }

            return dependenciesNode;
        }

        /// <summary>
        /// Creates or updates a project tree for a given IProjectDependenciesSubTreeProvider
        /// </summary>
        /// <param name="dependenciesNode"></param>
        /// <param name="subTreeProvider"></param>
        /// <param name="changes"></param>
        /// <param name="catalogs">Can be null if sub tree provider does not use design time build</param>
        /// <param name="cancellationToken"></param>
        /// <returns>IProjectTree for root Dependencies node</returns>
        private IProjectTree CreateOrUpdateSubTreeProviderNode(IProjectTree dependenciesNode,
                                                               IProjectDependenciesSubTreeProvider subTreeProvider,
                                                               IDependenciesChangeDiff changes,
                                                               IProjectCatalogSnapshot catalogs,
                                                               CancellationToken cancellationToken)
        {
            Requires.NotNull(dependenciesNode, nameof(dependenciesNode));
            Requires.NotNull(subTreeProvider, nameof(subTreeProvider));
            Requires.NotNull(subTreeProvider.RootNode, nameof(subTreeProvider.RootNode));
            Requires.NotNullOrEmpty(subTreeProvider.RootNode.Caption, nameof(subTreeProvider.RootNode.Caption));
            Requires.NotNullOrEmpty(subTreeProvider.ProviderType, nameof(subTreeProvider.ProviderType));

            var providerRootTreeNode = GetSubTreeRootNode(dependenciesNode,
                                                          subTreeProvider.RootNode.Flags);
            if (subTreeProvider.ShouldBeVisible)
            {
                bool newNode = false;
                if (providerRootTreeNode == null)
                {
                    providerRootTreeNode = NewTree(
                        caption: subTreeProvider.RootNode.Caption,
                        visible: true,
                        filePath: subTreeProvider.ProviderType,
                        browseObjectProperties: null,
                        flags: ProjectTreeFlags.Create(ProjectTreeFlags.Common.VirtualFolder.ToString(),
                                                       DependenciesProviderFlag,
                                                       subTreeProvider.ProviderType,
                                                       GenericDependencyNodeFlag)
                                               .Union(subTreeProvider.RootNode.Flags),
                        icon: subTreeProvider.RootNode.Icon.ToProjectSystemType(),
                        expandedIcon: subTreeProvider.RootNode.ExpandedIcon.ToProjectSystemType());

                    newNode = true;
                }

                if (changes != null)
                {
                    foreach (var removedItem in changes.RemovedNodes)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return dependenciesNode;
                        }

                        var node = providerRootTreeNode.FindNodeByName(removedItem.Caption);
                        if (node != null)
                        {
                            providerRootTreeNode = node.Remove();
                        }
                    }

                    var configuredProjectExports = GetActiveConfiguredProjectExports(ActiveConfiguredProject);
                    foreach (var addedItem in changes.AddedNodes)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return dependenciesNode;
                        }

                        Requires.NotNullOrEmpty(addedItem.Caption, nameof(addedItem.Caption));

                        var node = providerRootTreeNode.FindNodeByName(addedItem.Caption);
                        if (node == null)
                        {
                            IRule rule = null;
                            if (addedItem.Properties != null)
                            {
                                var itemContext = ProjectPropertiesContext.GetContext(UnconfiguredProject,
                                                                                      addedItem.ItemType,
                                                                                      addedItem.Caption);
                                rule = GetRuleForResolvableReference(
                                            itemContext,
                                            new KeyValuePair<string, IImmutableDictionary<string, string>>(
                                                addedItem.Caption, addedItem.Properties),
                                            catalogs,
                                            configuredProjectExports);
                            }

                            node = NewTree(caption: addedItem.Caption,
                                           visible: true,
                                           filePath: subTreeProvider.ProviderType,
                                           browseObjectProperties: rule,
                                           flags: ProjectTreeFlags.Create(ProjectTreeFlags.Common.VirtualFolder.ToString(),
                                                                          GenericDependencyNodeFlag)
                                                                  .Union(subTreeProvider.RootNode.Flags),
                                           icon: subTreeProvider.RootNode.Icon.ToProjectSystemType(),
                                           expandedIcon: subTreeProvider.RootNode.ExpandedIcon.ToProjectSystemType());

                            providerRootTreeNode = providerRootTreeNode.Add(node).Parent;
                        }
                    }
                }

                if (newNode)
                {
                    dependenciesNode = dependenciesNode.Add(providerRootTreeNode).Parent;
                }
                else
                {
                    dependenciesNode = providerRootTreeNode.Parent;
                }
            }
            else
            {
                if (providerRootTreeNode != null)
                {
                    dependenciesNode = dependenciesNode.Remove(providerRootTreeNode);
                }
            }

            return dependenciesNode;
        }

        /// <summary>
        /// Gets an unresolved reference for a given resolved reference.
        /// </summary>
        /// <param name="resolvedReferences">The collection of resolved references.</param>
        /// <param name="resolvedItemSpec">The full path to the resolved reference.</param>
        /// <param name="unresolvedReferenceSnapshots">
        /// The collection of snapshots that contain unresolved items.
        /// </param>
        /// <param name="catalogs">The snapshot of catalogs.</param>
        /// <param name="configuredProjectExports">
        /// Imports satisfied by the current active project configuration.
        /// </param>
        /// <returns>The description of how to find the unresolved item in the project.</returns>
        private IProjectPropertiesContext GetUnresolvedReference(
                    IProjectRuleSnapshot resolvedReferences, 
                    string resolvedItemSpec, 
                    IEnumerable<IProjectRuleSnapshot> unresolvedReferenceSnapshots, 
                    IProjectCatalogSnapshot catalogs, 
                    ConfiguredProjectExports configuredProjectExports)
        {
            Requires.NotNull(resolvedReferences, nameof(resolvedReferences));
            Requires.NotNullOrEmpty(resolvedItemSpec, nameof(resolvedItemSpec));
            Requires.NotNull(unresolvedReferenceSnapshots, nameof(unresolvedReferenceSnapshots));
            Requires.NotNull(catalogs, nameof(catalogs));
            Requires.NotNull(configuredProjectExports, nameof(configuredProjectExports));

            string unresolvedReferenceItemSpec = 
                    resolvedReferences.Items[resolvedItemSpec][ResolvedAssemblyReference.OriginalItemSpecProperty];
            foreach (var referenceSnapshot in unresolvedReferenceSnapshots)
            {
                if (referenceSnapshot.Items.ContainsKey(unresolvedReferenceItemSpec))
                {
                    string itemType = configuredProjectExports.GetItemTypeFromRuleName(referenceSnapshot.RuleName, 
                                                                                       catalogs, 
                                                                                       true);
                    if (itemType != null)
                    {
                        return ProjectPropertiesContext.GetContext(UnconfiguredProject, 
                                                                   itemType, 
                                                                   unresolvedReferenceItemSpec);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Handles changes to the references items in the project and updates the project tree.
        /// </summary>
        /// <param name="e">A description of the changes made to the project.</param>
        private void ProjectSubscriptionService_Changed(IProjectVersionedValue<
                                                            Tuple<IProjectSubscriptionUpdate, 
                                                                  IProjectCatalogSnapshot, 
                                                                  IProjectSharedFoldersSnapshot>> e)
        {
            TraceUtilities.TraceVerbose(
                "Configured project got an update in the ProjectSubscriptionService. Active configuration is: {0}", 
                ActiveConfiguredProject.ProjectConfiguration.Name);
            
            // store latest named schema catalogs
            NamedCatalogs = e.Value.Item2.NamedCatalogs;

            SubmitTreeUpdateAsync(
                (treeSnapshot, configuredProjectExports, ct) =>
                {
                    TraceUtilities.TraceVerbose(
                        "Updating the tree in the ProjectSubscriptionService. Active configuration is: {0}", 
                        ActiveConfiguredProject.ProjectConfiguration.Name);

                    IProjectTree assembliesNode;
                    var dependenciesNode = treeSnapshot.Value.Tree;
                    var changes = e.Value.Item1.ProjectChanges;
                    var catalogs = e.Value.Item2;

                    if (!_initialFillCompleted)
                    {
                        assembliesNode = CreateAssembliesNode(dependenciesNode);
                        TraceUtilities.TraceVerbose(
                            "Initial fill not completed creating references folder. Tree caption: {0}", 
                            assembliesNode.Caption);
                    }
                    else
                    {
                        assembliesNode = GetSubTreeRootNode(dependenciesNode, 
                                                            ProjectTreeFlags.Create(AssembliesSubTreeRootNodeFlag));
                    }


                    var resolvedReferenceChanges = ResolvedReferenceRuleNames.Select(ruleName => changes[ruleName])
                                                                             .ToImmutableHashSet();
                    IProjectRuleSnapshot[] resolvedReferences = resolvedReferenceChanges.Select(c => c.After)
                                                                                        .ToArray();

                    // Get a dictionary of rules to changes without resolved reference rules.
                    var unresolvedReferenceSnapshots = changes.Values
                        .Where(cd => !ResolvedReferenceRuleNames.Any(ruleName => 
                                        string.Equals(ruleName, 
                                                      cd.After.RuleName, 
                                                      StringComparison.OrdinalIgnoreCase)))
                        .ToDictionary(d => d.After.RuleName, d => d, StringComparer.OrdinalIgnoreCase);

                    foreach (var change in unresolvedReferenceSnapshots.Values)
                    {
                        string itemType = configuredProjectExports.GetItemTypeFromRuleName(change.After.RuleName, 
                                                                                           catalogs, 
                                                                                           true);
                        if (itemType == null)
                        {
                            // We must be missing that rule. Skip it.
                            continue;
                        }

                        foreach (string removedItem in change.Difference.RemovedItems)
                        {
                            var node = FindReferenceNode(assembliesNode, itemType, removedItem);
                            Report.IfNot(node != null, "Unable to find reference node to delete.");
                            if (node != null)
                            {
                                assembliesNode = node.Remove();
                            }
                        }

                        foreach (string addedItem in change.Difference.AddedItems)
                        {
                            var referenceNode = CreateOrUpdateDependencyNode(itemType, 
                                                                            addedItem, 
                                                                            resolvedReferences, 
                                                                            ref assembliesNode, 
                                                                            null /*oldNode*/, 
                                                                            catalogs, 
                                                                            configuredProjectExports);
                            assembliesNode = assembliesNode.Add(referenceNode).Parent;
                        }
                    }

                    foreach (var resolvedReferenceRuleChanges in resolvedReferenceChanges)
                    {
                        // We process removals before adds because if a reference changed identities, but
                        // still has the same OriginalItemSpec metadata, we want that to show as a successfully
                        // resolved reference.
                        foreach (string removedItem in resolvedReferenceRuleChanges.Difference.RemovedItems)
                        {
                            assembliesNode = UpdateReferenceNodeWithResolvedState(assembliesNode, 
                                                                                  unresolvedReferenceSnapshots, 
                                                                                  resolvedReferenceRuleChanges.Before, 
                                                                                  false /* resolved */, 
                                                                                  removedItem, 
                                                                                  catalogs, 
                                                                                  configuredProjectExports);
                        }

                        foreach (string addedItem in resolvedReferenceRuleChanges.Difference.AddedItems)
                        {
                            assembliesNode = UpdateReferenceNodeWithResolvedState(assembliesNode, 
                                                                                  unresolvedReferenceSnapshots, 
                                                                                  resolvedReferenceRuleChanges.After, 
                                                                                  true /* resolved */, 
                                                                                  addedItem, 
                                                                                  catalogs, 
                                                                                  configuredProjectExports);
                        }
                    }

                    assembliesNode = UpdateSharedImportReferenceNodes(assembliesNode, e.Value.Item3);

                    var dataSources = e.DataSourceVersions
                        // We're excluding this data source for now until we figure out how the 
                        // PhysicalProjectTreeProvider can reconcile grafts with different ActiveProjectConfiguration
                        // versions. With the below line we had bugs like 535352 where the product would flag an 
                        // internal error because this version would "decrement" across an active project config change.
                        .Add(ProjectTreeDataSources.ReferencesFolderProjectSnapshotVersion, 0L);
                    var result = Task.FromResult(new TreeUpdateResult(assembliesNode.Parent, true, dataSources));
                    if (!_initialFillCompleted)
                    {
                        //CodeMarkers.Instance.CodeMarker(CodeMarkerEvent.perfCpsProjectLoadReferencesPopulated);
                        _initialFillCompleted = true;
                        TraceUtilities.TraceVerbose("Initial fill completed for the references node.");
                    }

                    return result;
                });
        }

        /// <summary>
        /// Updates a reference node based on whether it resolved or not.
        /// </summary>
        /// <param name="tree">The References subtree to modify.</param>
        /// <param name="unresolvedReferenceSnapshots">
        /// The collection of snapshots that contain unresolved reference items.
        /// </param>
        /// <param name="resolvedReferences">The collection of resolved reference items.</param>
        /// <param name="resolved"></param> 
        /// <param name="resolvedItemSpec">
        /// The item name of the resolved reference whose tree node should be updated.
        /// </param>
        /// <param name="catalogs"></param> 
        /// <param name="configuredProjectExports"></param> 
        /// <returns>The modified references tree.</returns>
        private IProjectTree UpdateReferenceNodeWithResolvedState(
                                IProjectTree tree, 
                                Dictionary<string, IProjectChangeDescription> unresolvedReferenceSnapshots, 
                                IProjectRuleSnapshot resolvedReferences, 
                                bool resolved, 
                                string resolvedItemSpec, 
                                IProjectCatalogSnapshot catalogs, 
                                ConfiguredProjectExports configuredProjectExports)
        {
            var unresolvedItemContext = GetUnresolvedReference(resolvedReferences, resolvedItemSpec, 
                unresolvedReferenceSnapshots.Values.Select(cd => cd.After), catalogs, configuredProjectExports);
            if (unresolvedItemContext != null) // some refs like mscorlib don't always have an unresolved project item
            {
                var node = FindReferenceNode(tree, unresolvedItemContext.ItemType, unresolvedItemContext.ItemName);
                Report.IfNot(node != null, "Unable to find reference item \"{0}\".", unresolvedItemContext.ItemName);
                if (node != null)
                {
                    var resolvedReferenceSnapshots = resolved 
                            ? new IProjectRuleSnapshot[] { resolvedReferences } 
                            : new IProjectRuleSnapshot[0];
                    var updatedNode = CreateOrUpdateDependencyNode(unresolvedItemContext.ItemType, 
                                                                  unresolvedItemContext.ItemName, 
                                                                  resolvedReferenceSnapshots, 
                                                                  ref tree, 
                                                                  node, 
                                                                  catalogs, 
                                                                  configuredProjectExports);
                    tree = node.Replace(updatedNode).Parent;
                }
            }

            return tree;
        }

        /// <summary>
        /// Creates a node to represent a given dependency.
        /// </summary>
        /// <returns>A tree node.</returns>
        private IProjectItemTree CreateOrUpdateDependencyNode(string unresolvedReferenceItemType, 
                                                             string unresolvedReferenceItemSpec, 
                                                             IProjectRuleSnapshot[] resolvedReferences, 
                                                             ref IProjectTree parentNode, 
                                                             IProjectTree oldNode, 
                                                             IProjectCatalogSnapshot catalogs, 
                                                             ConfiguredProjectExports configuredProjectExports)
        {
            Requires.NotNullOrEmpty(unresolvedReferenceItemSpec, nameof(unresolvedReferenceItemSpec));
            Requires.NotNull(resolvedReferences, nameof(resolvedReferences));
            Requires.NotNull(catalogs, nameof(catalogs));
            Requires.NotNull(configuredProjectExports, nameof(configuredProjectExports));

            var itemContext = ProjectPropertiesContext.GetContext(UnconfiguredProject, unresolvedReferenceItemType, unresolvedReferenceItemSpec);
            var resolvedReference = GetResolvedReference(resolvedReferences, unresolvedReferenceItemType, unresolvedReferenceItemSpec);

            ProjectImageMoniker iconMoniker;
            string caption = string.Empty; // Make the compiler happy. caption will always be set.
            IRule rule;
            ProjectTreeFlags flags;
            bool isSdkReference = string.Equals(unresolvedReferenceItemType, 
                                                SdkReference.SchemaName, 
                                                StringComparison.OrdinalIgnoreCase); 
            bool isProjectReference = string.Equals(unresolvedReferenceItemType, 
                                                    ProjectReference.SchemaName, 
                                                    StringComparison.OrdinalIgnoreCase);

            // Did the reference resolve?
            if (resolvedReference.HasValue)
            {
                if (isSdkReference)
                {
                    resolvedReference.Value.Value.TryGetValue(ResolvedSdkReference.DisplayNameProperty, out caption);
                }
                else if (isProjectReference)
                {
                    caption = Path.GetFileNameWithoutExtension(unresolvedReferenceItemSpec);
                }
                else
                {
                    string fusionName;
                    if (resolvedReference.Value.Value.TryGetValue(ResolvedAssemblyReference.FusionNameProperty, 
                                                                  out fusionName) 
                        && !string.IsNullOrEmpty(fusionName))
                    {
                        var assemblyName = new AssemblyName(fusionName);
                        caption = assemblyName.Name;
                    }
                    else
                    {
                        caption = Path.GetFileNameWithoutExtension(resolvedReference.Value.Key);
                    }
                }

                iconMoniker = KnownMonikers.Reference.ToProjectSystemType();
                
                rule = GetRuleForResolvableReference(itemContext, 
                                                     resolvedReference.Value, 
                                                     catalogs, 
                                                     configuredProjectExports);
                flags = ResolvedReferenceFlags;
            }
            else
            {
                if (!isSdkReference)
                {
                    if (isProjectReference)
                    {
                        caption = Path.GetFileNameWithoutExtension(unresolvedReferenceItemSpec);
                    }
                    else
                    {
                        try
                        {
                            // We don't trim extension from here because usually there is no extension, and trimming it
                            // would make "System.Xml" look like "System".
                            caption = Path.GetFileName(unresolvedReferenceItemSpec);
                        }
                        catch (ArgumentException)
                        {
                            caption = unresolvedReferenceItemSpec;
                        }
                    }
                }

                iconMoniker = KnownMonikers.ReferenceWarning.ToProjectSystemType();
                rule = GetRuleForUnresolvableReference(itemContext, catalogs, configuredProjectExports);
                flags = UnresolvedReferenceFlags;
            }

            // We have an sdk reference but no display name so we therefore can only just parse
            // the identity to get the name.
            if (isSdkReference && string.IsNullOrEmpty(caption))
            {
                caption = (oldNode == null)
                    ? unresolvedReferenceItemSpec.Split(CommonConstants.CommaDelimiter)[0]
                    : oldNode.Caption;
            }

            // Now that we have the default caption lets see if that caption already exists in the tree and if 
            // so we need to alias it.
            Assumes.NotNullOrEmpty(caption);

            // Do we already have a caption with the same name?  If so, then we need to add (UnresolvedItemSpec) 
            // to the display name to disambiguate it.
            bool shouldAlias = false;
            IProjectTree matchingChild = null;
            shouldAlias = parentNode.TryFindImmediateChild(caption, out matchingChild);

            // If we couldn't find it by name, we'll need to see if we can find one that's already been aliased.
            if (!shouldAlias)
            {
                // See if we already have an item that starts with the same display name and also includes the
                // unresolvedItemSpec. This can only happen if we've already aliased it (see below):
                var aliasedSDKs = from childNode in parentNode.Children.OfType<IProjectItemTree>()
                                  where childNode.Caption.Equals(
                                                              string.Format(CultureInfo.CurrentCulture, "{0} ({1})", 
                                                                            caption, 
                                                                            childNode.
                                                                            Item.ItemName), 
                                                              StringComparison.OrdinalIgnoreCase)
                                  select childNode;

                shouldAlias = aliasedSDKs.Any();
            }

            // We do not want to alias if the matching node we found was ourselves.
            if (shouldAlias 
                && (oldNode == null || matchingChild == null || (oldNode.Identity != matchingChild.Identity)))
            {
                // Alias the caption for this node:
                caption = string.Format(CultureInfo.CurrentCulture, "{0} ({1})", caption, unresolvedReferenceItemSpec);

                IProjectItemTree matchingItemTree = matchingChild as IProjectItemTree;
                if (matchingItemTree != null)
                {
                    var matchingContext = GetCustomPropertyContext(matchingItemTree.Parent);
                    var matchingValues = new ReferencesProjectTreeCustomizablePropertyValues
                    {
                        Caption = string.Format(CultureInfo.CurrentCulture, "{0} ({1})", 
                                                matchingItemTree.Caption, 
                                                matchingItemTree.Item.ItemName),
                        Flags = matchingItemTree.Flags,
                        Icon = matchingItemTree.Icon,
                        ExpandedIcon = matchingItemTree.ExpandedIcon
                    };

                    ApplyProjectTreePropertiesCustomization(matchingContext, matchingValues);

                    matchingItemTree = matchingItemTree.SetProperties(
                        caption: matchingValues.Caption,
                        flags: matchingValues.Flags,
                        icon: matchingValues.Icon,
                        expandedIcon: matchingValues.ExpandedIcon);

                    parentNode = matchingItemTree.Parent;
                }
            }

            var context = GetCustomPropertyContext(parentNode);
            var values = new ReferencesProjectTreeCustomizablePropertyValues
            {
                Caption = caption,
                Flags = flags,
                Icon = iconMoniker
            };

            ApplyProjectTreePropertiesCustomization(context, values);

            if (oldNode == null)
            {
                return NewTree(
                        values.Caption,
                        itemContext,
                        propertySheet: null,
                        browseObjectProperties: rule,
                        flags: values.Flags,
                        icon: values.Icon);
            }
            else
            {
                var icon = iconMoniker;
                return (IProjectItemTree)oldNode.SetProperties(
                    context: itemContext,
                    propertySheet: null,
                    isLinked: false,
                    caption: values.Caption,
                    browseObjectProperties: rule,
                    resetBrowseObjectProperties: rule == null,
                    flags: values.Flags,
                    icon: values.Icon,
                    resetIcon: icon == null);
            }
        }

        private IImmutableDictionary<string, IPropertyPagesCatalog> GetNamedCatalogs(IProjectCatalogSnapshot catalogs)
        {
            if (catalogs != null)
            {
                return catalogs.NamedCatalogs;
            }

            if (NamedCatalogs != null)
            {
                return NamedCatalogs;
            }

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                // Note: it is unlikely that we end up here, however for cases when node providers
                // getting their node data not from Design time build events, we might have OnDependenciesChanged
                // event coming before initial design time build event updates NamedCatalogs in this class.
                // Thus, just in case, explicitly request it here (GetCatalogsAsync will accuire a project read lock)
                NamedCatalogs = await ActiveConfiguredProject.Services
                                                             .PropertyPagesCatalog
                                                             .GetCatalogsAsync(CancellationToken.None)
                                                             .ConfigureAwait(false);
            });

            return NamedCatalogs;
        }

        /// <summary>
        /// Gets the rule(s) that applies to a reference.
        /// </summary>
        /// <param name="itemType">The item type on the unresolved reference.</param>
        /// <param name="resolved">
        /// A value indicating whether to return rules for resolved or unresolved reference state.
        /// </param>
        /// <param name="namedCatalogs">The dictionary of catalogs.</param>
        /// <returns>The sequence of matching rules.  Hopefully having exactly one element.</returns>
        private IEnumerable<Rule> GetSchemaForReference(string itemType, 
                                                        bool resolved,
                                                        IImmutableDictionary<string, IPropertyPagesCatalog> namedCatalogs)
        {
            Requires.NotNull(namedCatalogs, nameof(namedCatalogs));

            var browseObjectCatalog = namedCatalogs[PropertyPageContexts.BrowseObject];
            return from schemaName in browseObjectCatalog.GetPropertyPagesSchemas(itemType)
                   let schema = browseObjectCatalog.GetSchema(schemaName)
                   where schema.DataSource != null 
                         && string.Equals(itemType, schema.DataSource.ItemType, StringComparison.OrdinalIgnoreCase)
                         && (resolved == string.Equals(schema.DataSource.SourceType, 
                                                       RuleDataSourceTypes.TargetResults, 
                                                       StringComparison.OrdinalIgnoreCase))
                   select schema;
        }

        /// <summary>
        /// Gets an IRule to attach to a project item so that browse object properties will be displayed.
        /// </summary>
        private IRule GetRuleForResolvableReference(
                        IProjectPropertiesContext unresolvedContext, 
                        KeyValuePair<string, IImmutableDictionary<string, string>> resolvedReference, 
                        IProjectCatalogSnapshot catalogs, 
                        ConfiguredProjectExports configuredProjectExports)
        {
            Requires.NotNull(unresolvedContext, nameof(unresolvedContext));

            var namedCatalogs = GetNamedCatalogs(catalogs);
            var schemas = GetSchemaForReference(unresolvedContext.ItemType, true, namedCatalogs).ToList();
            if (schemas.Count == 1)
            {
                IRule rule = configuredProjectExports.RuleFactory.CreateResolvedReferencePageRule(
                                schemas[0], 
                                unresolvedContext, 
                                resolvedReference.Key, 
                                resolvedReference.Value);
                return rule;
            }
            else
            {
                if (schemas.Count > 1)
                {
                    TraceUtilities.TraceWarning(
                        "Too many rule schemas ({0}) in the BrowseObject context were found.  Only 1 is allowed.", 
                        schemas.Count);
                }

                // Since we have no browse object, we still need to create *something* so that standard property 
                // pages can pop up.
                var emptyRule = RuleExtensions.SynthesizeEmptyRule(unresolvedContext.ItemType);
                return configuredProjectExports.PropertyPagesDataModelProvider.GetRule(
                            emptyRule, 
                            unresolvedContext.File, 
                            unresolvedContext.ItemType, 
                            unresolvedContext.ItemName);
            }
        }

        /// <summary>
        /// Gets an IRule to attach to a project item so that browse object properties will be displayed.
        /// </summary>
        private IRule GetRuleForUnresolvableReference(IProjectPropertiesContext unresolvedContext, 
                                                      IProjectCatalogSnapshot catalogs, 
                                                      ConfiguredProjectExports configuredProjectExports)
        {
            Requires.NotNull(unresolvedContext, nameof(unresolvedContext));
            Requires.NotNull(configuredProjectExports, nameof(configuredProjectExports));

            var namedCatalogs = GetNamedCatalogs(catalogs);
            var schemas = GetSchemaForReference(unresolvedContext.ItemType, false, namedCatalogs).ToList();
            if (schemas.Count == 1)
            {
                Requires.NotNull(namedCatalogs, nameof(namedCatalogs));
                var browseObjectCatalog = namedCatalogs[PropertyPageContexts.BrowseObject];
                return browseObjectCatalog.BindToContext(schemas[0].Name, unresolvedContext);
            }

            if (schemas.Count > 1)
            {
                TraceUtilities.TraceWarning(
                    "Too many rule schemas ({0}) in the BrowseObject context were found. Only 1 is allowed.", 
                    schemas.Count);
            }

            // Since we have no browse object, we still need to create *something* so that standard property 
            // pages can pop up.
            var emptyRule = RuleExtensions.SynthesizeEmptyRule(unresolvedContext.ItemType);
            return configuredProjectExports.PropertyPagesDataModelProvider.GetRule(
                        emptyRule, 
                        unresolvedContext.File, 
                        unresolvedContext.ItemType, 
                        unresolvedContext.ItemName);
        }

        /// <summary>
        /// Updates the shared import nodes that are shown under the 'References' node.
        /// </summary>
        /// <param name="tree">The current References tree.</param>
        /// <param name="sharedFolders">Snapshot of shared folders.</param>
        /// <returns>The updated References tree.</returns>
        private IProjectTree UpdateSharedImportReferenceNodes(IProjectTree tree, 
                                                              IProjectSharedFoldersSnapshot sharedFolders)
        {
            Requires.NotNull(tree, nameof(tree));
            Requires.NotNull(sharedFolders, nameof(sharedFolders));

            IEnumerable<string> sharedFolderProjectPaths = sharedFolders.Value.Select(sf => sf.ProjectPath);

            var currentSharedImportNodes = tree.Children
                .Where(childTree => childTree.Flags.Contains(ProjectTreeFlags.Common.SharedProjectImportReference));

            IEnumerable<string> currentSharedImportNodePaths = currentSharedImportNodes
                .Select(sharedProjectImportReferenceNode => sharedProjectImportReferenceNode.FilePath);

            IEnumerable<string> addedSharedImportPaths = sharedFolderProjectPaths.Except(currentSharedImportNodePaths);
            foreach (string addedSharedImportPath in addedSharedImportPaths)
            {
                var context = GetCustomPropertyContext(tree);
                var values = new ReferencesProjectTreeCustomizablePropertyValues
                {
                    Caption = Path.GetFileNameWithoutExtension(addedSharedImportPath),
                    Icon = KnownMonikers.SharedProject.ToProjectSystemType(),
                    Flags = SharedImportReferenceFlags
                };

                ApplyProjectTreePropertiesCustomization(context, values);

                IProjectTree newImportNode = NewTree(
                    values.Caption,
                    filePath: addedSharedImportPath,
                    icon: values.Icon,
                    expandedIcon: values.ExpandedIcon,
                    flags: values.Flags);

                tree = tree.Add(newImportNode).Parent;
            }

            IEnumerable<string> removedSharedImportPaths = 
                    currentSharedImportNodePaths.Except(sharedFolderProjectPaths);
            foreach (string removedSharedImportPath in removedSharedImportPaths)
            {
                IProjectTree existingImportNode = currentSharedImportNodes
                    .Where(node => PathHelper.IsSamePath(node.FilePath, removedSharedImportPath)).FirstOrDefault();

                if (existingImportNode != null)
                {
                    tree = tree.Remove(existingImportNode);
                }
            }

            return tree;
        }

        private ProjectTreeCustomizablePropertyContext GetCustomPropertyContext(IProjectTree parent)
        {
            return new ProjectTreeCustomizablePropertyContext
            {
                ExistsOnDisk = false,
                ParentNodeFlags = parent?.Flags ?? default(ProjectTreeFlags)
            };
        }

        private void ApplyProjectTreePropertiesCustomization(
                        IProjectTreeCustomizablePropertyContext context,
                        ReferencesProjectTreeCustomizablePropertyValues values)
        {
            foreach (var provider in ProjectTreePropertiesProviders.ExtensionValues())
            {
                provider.CalculatePropertyValues(context, values);
            }
        }

        protected void OnDependenciesChanged(object sender, DependenciesChangedEventArgs e)
        {
            var nowait = SubmitTreeUpdateAsync(
                (treeSnapshot, configuredProjectExports, cancellationToken) =>
                {
                    var dependenciesNode = treeSnapshot.Value.Tree;
                    dependenciesNode = CreateOrUpdateSubTreeProviderNode(dependenciesNode,
                                                                         e.Provider,
                                                                         changes: e.Changes,                                                                         
                                                                         cancellationToken: cancellationToken,
                                                                         catalogs: e.Catalogs);
                    return Task.FromResult(new TreeUpdateResult(dependenciesNode, true));
                });

            ProjectContextChanged?.Invoke(this, new ProjectContextEventArgs(this));
        }

        #region IDependenciesGraphProjectContext

        /// <summary>
        /// Returns a dependencies node sub tree provider for given dependency provider type.
        /// </summary>
        /// <param name="providerType">
        /// Type of the dependnecy. It is expected to be a unique string associated with a provider. 
        /// </param>
        /// <returns>
        /// Instance of <see cref="IProjectDependenciesSubTreeProvider"/> or null if there no provider 
        /// for given type.
        /// </returns>
        IProjectDependenciesSubTreeProvider IDependenciesGraphProjectContext.GetProvider(string providerType)
        {
            if (string.IsNullOrEmpty(providerType))
            {
                return null;
            }

            return SubTreeProviders.FirstOrDefault(x => providerType.Equals(x.Value.ProviderType, 
                                                                            StringComparison.OrdinalIgnoreCase))
                                   .Value;
        }

        IEnumerable<IProjectDependenciesSubTreeProvider> IDependenciesGraphProjectContext.GetProviders()
        {
            return SubTreeProviders.Select(x => x.Value).ToList();
        }

        /// <summary>
        /// Path to project file
        /// </summary>
        string IDependenciesGraphProjectContext.ProjectFilePath
        {
            get
            {
                return UnconfiguredProject.FullPath;
            }
        }

        /// <summary>
        /// Gets called when dependencies change
        /// </summary>
        public event ProjectContextEventHandler ProjectContextChanged;

        /// <summary>
        /// Gets called when project is unloading and dependencies subtree is disposing
        /// </summary>
        public event ProjectContextEventHandler ProjectContextUnloaded;

        #endregion

        /// <summary>
        /// Describes services collected from the active configured project.
        /// </summary>
        [Export]
        protected class MyConfiguredProjectExports : ConfiguredProjectExports
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MyConfiguredProjectExports"/> class.
            /// </summary>
            [ImportingConstructor]
            protected MyConfiguredProjectExports(ConfiguredProject configuredProject)
                : base(configuredProject)
            {
            }
        }

        /// <summary>
        /// A private implementation of <see cref="IProjectTreeCustomizablePropertyContext"/>.
        /// </summary>
        private class ProjectTreeCustomizablePropertyContext : IProjectTreeCustomizablePropertyContext
        {
            public string ItemName { get; set; }

            public string ItemType { get; set; }

            public IImmutableDictionary<string, string> Metadata { get; set; }

            public ProjectTreeFlags ParentNodeFlags { get; set; }

            public bool ExistsOnDisk { get; set; }

            public bool IsFolder
            {
                get
                {
                    return false;
                }
            }

            public bool IsNonFileSystemProjectItem
            {
                get
                {
                    return true;
                }
            }

            public IImmutableDictionary<string, string> ProjectTreeSettings { get; set; }
        }
    }
}
