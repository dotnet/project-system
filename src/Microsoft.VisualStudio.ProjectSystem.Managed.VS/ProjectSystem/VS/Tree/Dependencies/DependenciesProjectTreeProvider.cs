// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
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
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependenciesProjectTreeProvider : ProjectTreeProviderBase, 
                                                     IProjectTreeProvider, 
                                                     IDependenciesGraphProjectContext
    {
        private static readonly ProjectTreeFlags DependenciesRootNodeFlags
                = ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp
                                          | ProjectTreeFlags.Common.ReferencesFolder
                                          | ProjectTreeFlags.Common.VirtualFolder)
                                  .Add("DependenciesRootNode");

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
        public OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider> SubTreeProviders { get; }

        /// <summary>
        /// Keeps latest data source versions sent from each subtree provider. Is needed for merging and 
        /// posting consistent data source versions after we process tree updates.
        /// </summary>
        private Dictionary<string, IImmutableDictionary<NamedIdentity, IComparable>> _latestDataSourcesVersions =
            new Dictionary<string, IImmutableDictionary<NamedIdentity, IComparable>>(StringComparer.OrdinalIgnoreCase);

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
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether deleting a given set of items from the project, and optionally from disk,
        /// would be allowed. 
        /// Note: CanRemove can be called several times since there two types of remove operations:
        ///   - Remove is a command that can remove project tree items form the tree/project but not from disk. 
        ///     For that command requests deleteOptions has DeleteOptions.None flag.
        ///   - Delete is a command that can remove project tree items and form project and from disk. 
        ///     For this command requests deleteOptions has DeleteOptions.DeleteFromStorage flag.
        /// We can potentially support only Remove command here, since we don't remove Dependencies form disk, 
        /// thus we return false when DeleteOptions.DeleteFromStorage is provided.
        /// </summary>
        /// <param name="nodes">The nodes that should be deleted.</param>
        /// <param name="deleteOptions">
        /// A value indicating whether the items should be deleted from disk as well as from the project file.
        /// </param>
        public override bool CanRemove(IImmutableSet<IProjectTree> nodes, 
                                       DeleteOptions deleteOptions = DeleteOptions.None)
        {
            if (deleteOptions.HasFlag(DeleteOptions.DeleteFromStorage))
            {
                return false;
            }

            return nodes.All(node => (node.Flags.Contains(DependencyNode.GenericDependencyFlags)
                                        && node.BrowseObjectProperties != null
                                        && !node.Flags.Contains(DependencyNode.DoesNotSupportRemove)));
        }

        /// <summary>
        /// Deletes items from the project, and optionally from disk.
        /// Note: Delete and Remove commands are handled via IVsHierarchyDeleteHandler3, not by
        /// IAsyncCommandGroupHandler and first asks us we CanRemove nodes. If yes then RemoveAsync is called.
        /// We can remove only nodes that are standard and based on project items, i.e. nodes that 
        /// are created by default IProjectDependenciesSubTreeProvider implementations and have 
        /// DependencyNode.GenericDependencyFlags flags and IRule with Context != null, in order to obtain 
        /// node's itemSpec. ItemSpec then used to remove a project item having same Include.
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
            IEnumerable<IProjectTree> referenceItemNodes = nodes.Except(sharedImportNodes);

            using (var access = await ProjectLockService.WriteLockAsync())
            {
                var project = await access.GetProjectAsync(ActiveConfiguredProject).ConfigureAwait(true);

                // Handle the removal of normal reference Item Nodes (this excludes any shared import nodes).
                foreach (var node in referenceItemNodes)
                {
                    if (node.BrowseObjectProperties == null || node.BrowseObjectProperties.Context == null)
                    {
                        // if node does not have an IRule with valid ProjectPropertiesContext we can not 
                        // get it's itemsSpec. If nodes provided by custom IProjectDependenciesSubTreeProvider
                        // implementation, and have some custom IRule without context, it is not a problem,
                        // since they wouldnot have DependencyNode.GenericDependencyFlags and we would not 
                        // end up here, since CanRemove would return false and Remove command would not show 
                        // up for those nodes. 
                        continue;
                    }

                    var nodeItemContext = node.BrowseObjectProperties.Context;
                    var unresolvedReferenceItem = project.GetItemsByEvaluatedInclude(nodeItemContext.ItemName)
                        .FirstOrDefault(item => string.Equals(item.ItemType, 
                                                              nodeItemContext.ItemType, 
                                                              StringComparison.OrdinalIgnoreCase));

                    Report.IfNot(unresolvedReferenceItem != null, "Cannot find reference to remove.");
                    if (unresolvedReferenceItem != null)
                    {
                        await access.CheckoutAsync(unresolvedReferenceItem.Xml.ContainingProject.FullPath)
                                    .ConfigureAwait(true);
                        project.RemoveItem(unresolvedReferenceItem);
                    }
                }

                // Handle the removal of shared import nodes.
                var projectXml = await access.GetProjectXmlAsync(UnconfiguredProject.FullPath)
                                             .ConfigureAwait(true);
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
                                        .ConfigureAwait(true);
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
        /// specified in the project file)
        /// </summary>
        public override IProjectTree FindByPath(IProjectTree root, string path)
        {
            var dependenciesNode = GetSubTreeRootNode(root, DependenciesRootNodeFlags);
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
                                        dependenciesNode = CreateOrUpdateSubTreeProviderNodes(dependenciesNode, 
                                                                                              cancellationToken);

                                        return Task.FromResult(new TreeUpdateResult(dependenciesNode, true));
                                    });                            
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
                foreach (var provider in SubTreeProviders)
                {
                    provider.Value.DependenciesChanged -= OnDependenciesChanged;
                }

                ProjectContextUnloaded?.Invoke(this, new ProjectContextEventArgs(this));
            }

            base.Dispose(disposing);
        }

        private void OnDependenciesChanged(object sender, DependenciesChangedEventArgs e)
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
                    dependenciesNode = RefreshDependentProvidersNodes(dependenciesNode, e.Provider);

                    ProjectContextChanged?.Invoke(this, new ProjectContextEventArgs(this));

                    // TODO We still are getting mismatched data sources and need to figure out better 
                    // way of merging, mute them for now and get to it in U1
                    return Task.FromResult(new TreeUpdateResult(dependenciesNode, 
                                                                false, 
                                                                null /*GetMergedDataSourceVersions(e)*/));
                });
        }

        private IProjectTree RefreshDependentProvidersNodes(IProjectTree dependenciesNode, 
                                                            IProjectDependenciesSubTreeProvider changedProvider)
        {
            foreach (var subTreeProvider in SubTreeProviders)
            {
                var providerRootTreeNode = GetSubTreeRootNode(dependenciesNode,
                                      subTreeProvider.Value.RootNode.Flags);
                if (providerRootTreeNode == null)
                {
                    continue;
                }

                var provider = subTreeProvider.Value as DependenciesSubTreeProviderBase;
                if (provider == null || !provider.CanDependOnProvider(changedProvider))
                {
                    continue;
                }

                var newProviderNode = providerRootTreeNode;
                foreach (var treeNode in providerRootTreeNode.Children)
                {
                    if (!treeNode.Flags.Contains(DependencyNode.DependsOnOtherProviders))
                    {
                        continue;
                    }

                    var newNode = treeNode;
                    newProviderNode = newProviderNode.Remove(treeNode);
                    newProviderNode = newProviderNode.Add(treeNode).Parent;
                }

                dependenciesNode = newProviderNode.Parent;
            }

            return dependenciesNode;
        }

        /// <summary>
        /// Note: this is important to merge data source versions correctly here, since 
        /// and different providers sending this event might have different processing time 
        /// and we might end up in later data source versions coming before earlier ones. If 
        /// we post greater versions before lower ones there will be exception and data flow 
        /// might be broken after that. 
        /// Another reason post data source versions here is that there could be other 
        /// components waiting for Dependencies tree changes and if we don't post versions,
        /// they could not track our changes.
        /// </summary>
        private IImmutableDictionary<NamedIdentity, IComparable> GetMergedDataSourceVersions(
                    DependenciesChangedEventArgs e)
        {
            IImmutableDictionary<NamedIdentity, IComparable> mergedDataSourcesVersions = null;
            lock (_latestDataSourcesVersions)
            {
                if (!string.IsNullOrEmpty(e.Provider.ProviderType) && e.DataSourceVersions != null)
                {
                    _latestDataSourcesVersions[e.Provider.ProviderType] = e.DataSourceVersions;
                }

                mergedDataSourcesVersions = 
                    ProjectDataSources.MergeDataSourceVersions(_latestDataSourcesVersions.Values);
            }

            return mergedDataSourcesVersions;
        }

        /// <summary>
        /// Creates the loading References folder node.
        /// </summary>
        /// <returns>a new "Dependencies" tree node.</returns>
        private IProjectTree CreateDependenciesFolder(IProjectTree oldNode)
        {
            if (oldNode == null)
            {
                var values = new ReferencesProjectTreeCustomizablePropertyValues
                {
                    Caption = VSResources.DependenciesNodeName,
                    Icon = KnownMonikers.Reference.ToProjectSystemType(),
                    ExpandedIcon = KnownMonikers.Reference.ToProjectSystemType(),
                    Flags = DependenciesRootNodeFlags
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

        private IProjectItemTree CreateProjectItemTreeNode(IProjectTree providerRootTreeNode, 
                                                           IDependencyNode nodeInfo,
                                                           IProjectCatalogSnapshot catalogs)
        {
            var isGenericNodeType = nodeInfo.Flags.Contains(DependencyNode.GenericDependencyFlags);
            var properties = nodeInfo.Properties ??
                    ImmutableDictionary<string, string>.Empty
                                                       .Add(Folder.IdentityProperty, nodeInfo.Caption)
                                                       .Add(Folder.FullPathProperty, string.Empty);

            // For generic node types we do set correct, known item types, however for custom nodes
            // provided by third party extensions we can not guarantee that item type will be known. 
            // Thus always set predefined itemType for all custom nodes.
            // TODO: generate specific xaml rule for generic Dependency nodes
            // tracking issue: https://github.com/dotnet/roslyn-project-system/issues/1102
            var itemType = isGenericNodeType ? nodeInfo.Id.ItemType : Folder.SchemaName;
            
            // when itemSpec is not in valid absolute path format, property page does not show 
            // item name correctly. Use real Name for the node here instead of caption, since caption
            // can have other info like version in it.
            var itemSpec = nodeInfo.Flags.Contains(DependencyNode.CustomItemSpec)
                    ? nodeInfo.Name
                    : nodeInfo.Id.ItemSpec;
            var itemContext = ProjectPropertiesContext.GetContext(UnconfiguredProject, itemType, itemSpec);
            var configuredProjectExports = GetActiveConfiguredProjectExports(ActiveConfiguredProject);

            IRule rule = null;
            if (nodeInfo.Resolved || !isGenericNodeType)
            {
                rule = GetRuleForResolvableReference(
                            itemContext,
                            new KeyValuePair<string, IImmutableDictionary<string, string>>(
                                itemSpec, properties),
                            catalogs,
                            configuredProjectExports,
                            isGenericNodeType);
            }
            else
            {
                rule = GetRuleForUnresolvableReference(
                            itemContext,
                            catalogs,
                            configuredProjectExports);
            }

            // Notify about tree changes to customization context
            var customTreePropertyContext = GetCustomPropertyContext(providerRootTreeNode);
            var customTreePropertyValues = new ReferencesProjectTreeCustomizablePropertyValues
            {
                Caption = nodeInfo.Caption,
                Flags = nodeInfo.Flags,
                Icon = nodeInfo.Icon.ToProjectSystemType()
            };

            ApplyProjectTreePropertiesCustomization(customTreePropertyContext, customTreePropertyValues);

            var treeItemNode = NewTree(caption: nodeInfo.Caption,
                                item: itemContext,
                                propertySheet: null,
                                visible: true,
                                browseObjectProperties: rule,
                                flags: nodeInfo.Flags,
                                icon: nodeInfo.Icon.ToProjectSystemType(),
                                expandedIcon: nodeInfo.ExpandedIcon.ToProjectSystemType());

            return treeItemNode;
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

            var projectFolder = Path.GetDirectoryName(UnconfiguredProject.FullPath);                                    
            var providerRootTreeNode = GetSubTreeRootNode(dependenciesNode,
                                                          subTreeProvider.RootNode.Flags);
            if (subTreeProvider.RootNode.HasChildren || subTreeProvider.ShouldBeVisibleWhenEmpty)
            {
                bool newNode = false;
                if (providerRootTreeNode == null)
                {
                    providerRootTreeNode = NewTree(
                        caption: subTreeProvider.RootNode.Caption,
                        visible: true,
                        filePath: subTreeProvider.RootNode.Id.ToString(),
                        browseObjectProperties: null,
                        flags: subTreeProvider.RootNode.Flags,
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

                        var treeNode = FindProjectTreeNode(providerRootTreeNode, removedItem, projectFolder);
                        if (treeNode != null)
                        {
                            providerRootTreeNode = treeNode.Remove();
                        }
                    }

                    foreach (var updatedItem in changes.UpdatedNodes)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return dependenciesNode;
                        }

                        var treeNode = FindProjectTreeNode(providerRootTreeNode, updatedItem, projectFolder);
                        if (treeNode != null)
                        {

                            var updatedNodeParentContext = GetCustomPropertyContext(treeNode.Parent);
                            var updatedValues = new ReferencesProjectTreeCustomizablePropertyValues
                            {
                                Caption = updatedItem.Caption,
                                Flags = updatedItem.Flags,
                                Icon = updatedItem.Icon.ToProjectSystemType(),
                                ExpandedIcon = updatedItem.ExpandedIcon.ToProjectSystemType()
                            };

                            ApplyProjectTreePropertiesCustomization(updatedNodeParentContext, updatedValues);

                            // update existing tree node properties
                            treeNode = treeNode.SetProperties(
                                caption: updatedItem.Caption,
                                flags: updatedItem.Flags,
                                icon: updatedItem.Icon.ToProjectSystemType(),
                                expandedIcon: updatedItem.ExpandedIcon.ToProjectSystemType());

                            providerRootTreeNode = treeNode.Parent;
                        }
                    }

                    foreach (var addedItem in changes.AddedNodes)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return dependenciesNode;
                        }

                        var treeNode = FindProjectTreeNode(providerRootTreeNode, addedItem, projectFolder);
                        if (treeNode == null)
                        {
                            treeNode = CreateProjectItemTreeNode(providerRootTreeNode, addedItem, catalogs);

                            providerRootTreeNode = providerRootTreeNode.Add(treeNode).Parent;
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
        /// Finds IProjectTree node in the top level children of a given parent IProjectTree node.
        /// Depending on the type of IDependencyNode search method is different:
        ///     - if dependency node has custom ItemSpec, we only can find it by caption.
        ///     - if dependency node has normal ItemSpec, we first try to find it by path and then
        ///       by caption if path was not found (since unresolved and resolved items can have 
        ///       different items specs).
        /// </summary>
        private IProjectTree FindProjectTreeNode(IProjectTree parentNode, 
                                                 IDependencyNode nodeInfo, 
                                                 string projectFolder)
        {
            IProjectTree treeNode = null;
            if (nodeInfo.Flags.Contains(DependencyNode.CustomItemSpec))
            {
                treeNode = parentNode.FindNodeByCaption(nodeInfo.Caption);
            }
            else
            {
                var itemSpec = nodeInfo.Id.ItemSpec;
                if (!ManagedPathHelper.IsRooted(itemSpec))
                {
                    itemSpec = ManagedPathHelper.TryMakeRooted(projectFolder, itemSpec);
                }

                if (!string.IsNullOrEmpty(itemSpec))
                {
                    treeNode = parentNode.FindNodeByPath(itemSpec);
                }

                if (treeNode == null)
                {
                    treeNode = parentNode.FindNodeByCaption(nodeInfo.Caption);
                }
            }

            return treeNode;
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
                    if (referencePath.Value.TryGetValue(ResolvedAssemblyReference.OriginalItemSpecProperty,
                                                        out string originalItemSpec)
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

            // Note: usually for default/generic dependencies we have sets of 2 rule schemas:
            //  - rule for unresolved dependency, they persist in ProjectFile
            //  - rule for resolved dependency, they persist in ResolvedReference
            // So to be able to find rule for resolved or unresolved reference we need to be consistent there 
            // and make sure we do set ResolvedReference persistense for resolved dependnecies, since we rely
            // on that here when pick correct rule schema.
            // (old code used to check if rule datasource has SourceType=TargetResults, which was true for Resolved,
            // dependencies. However now we have custom logic for collecting unresolved dependencies too and use 
            // DesignTime build results there too. Thats why we swicthed to check for persistence).
            var browseObjectCatalog = namedCatalogs[PropertyPageContexts.BrowseObject];
            return from schemaName in browseObjectCatalog.GetPropertyPagesSchemas(itemType)
                   let schema = browseObjectCatalog.GetSchema(schemaName)
                   where schema.DataSource != null 
                         && string.Equals(itemType, schema.DataSource.ItemType, StringComparison.OrdinalIgnoreCase)
                         && (resolved == string.Equals(schema.DataSource.Persistence,
                                                       RuleDataSourceTypes.PersistenceResolvedReference,
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
                        ConfiguredProjectExports configuredProjectExports,
                        bool isGenericDependency = true)
        {
            Requires.NotNull(unresolvedContext, nameof(unresolvedContext));

            var namedCatalogs = GetNamedCatalogs(catalogs);
            var schemas = GetSchemaForReference(unresolvedContext.ItemType, isGenericDependency, namedCatalogs).ToList();
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

        /// <summary>
        /// Creates a new instance of the configured project exports class.
        /// </summary>
        protected override ConfiguredProjectExports GetActiveConfiguredProjectExports(
                                ConfiguredProject newActiveConfiguredProject)
        {
            Requires.NotNull(newActiveConfiguredProject, nameof(newActiveConfiguredProject));

            return GetActiveConfiguredProjectExports<MyConfiguredProjectExports>(newActiveConfiguredProject);
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

            var lazyProvider = SubTreeProviders.FirstOrDefault(x => providerType.Equals(x.Value.ProviderType,
                                                                            StringComparison.OrdinalIgnoreCase));
            if (lazyProvider == null)
            {
                return null;
            }

            return lazyProvider.Value;
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
        public event EventHandler<ProjectContextEventArgs> ProjectContextChanged;

        /// <summary>
        /// Gets called when project is unloading and dependencies subtree is disposing
        /// </summary>
        public event EventHandler<ProjectContextEventArgs> ProjectContextUnloaded;

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
