// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides the special "Dependencies" folder to project trees.
    /// </summary>
    [Export(ExportContractNames.ProjectTreeProviders.PhysicalViewRootGraft, typeof(IProjectTreeProvider))]
    [Export(typeof(IDependenciesTreeServices))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependenciesProjectTreeProvider :
        ProjectTreeProviderBase,
        IProjectTreeProvider,
        IDependenciesTreeServices
    {
        private readonly object _treeUpdateLock = new object();
        private Task _treeUpdateQueueTask = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependenciesProjectTreeProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        public DependenciesProjectTreeProvider(
            IProjectThreadingService threadingService,
            UnconfiguredProject unconfiguredProject,
            IDependenciesSnapshotProvider dependenciesSnapshotProvider,
            [Import(DependencySubscriptionsHost.DependencySubscriptionsHostContract)]
            ICrossTargetSubscriptionsHost dependenciesHost,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService)
            : base(threadingService, unconfiguredProject)
        {
            ProjectTreePropertiesProviders = new OrderPrecedenceImportCollection<IProjectTreePropertiesProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: unconfiguredProject);

            ViewProviders = new OrderPrecedenceImportCollection<IDependenciesTreeViewProvider>(
                                    ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst,
                                    projectCapabilityCheckProvider: unconfiguredProject);

            DependenciesSnapshotProvider = dependenciesSnapshotProvider;
            DependenciesHost = dependenciesHost;
            TasksService = tasksService;

            unconfiguredProject.ProjectUnloading += OnUnconfiguredProjectUnloading;
        }

        /// <summary>
        /// Gets the collection of <see cref="IProjectTreePropertiesProvider"/> imports 
        /// that apply to the references tree.
        /// </summary>
        [ImportMany(ReferencesProjectTreeCustomizablePropertyValues.ContractName)]
        private OrderPrecedenceImportCollection<IProjectTreePropertiesProvider> ProjectTreePropertiesProviders { get; }

        [ImportMany]
        private OrderPrecedenceImportCollection<IDependenciesTreeViewProvider> ViewProviders { get; }

        private ICrossTargetSubscriptionsHost DependenciesHost { get; }

        private IDependenciesSnapshotProvider DependenciesSnapshotProvider { get; }

        private IProjectAsynchronousTasksService TasksService { get; }

        /// <summary>
        /// Keeps latest updated snapshot of all rules schema catalogs
        /// </summary>
        private IImmutableDictionary<string, IPropertyPagesCatalog> NamedCatalogs { get; set; }

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

            var snapshot = DependenciesSnapshotProvider.CurrentSnapshot;
            if (snapshot == null)
            {
                return false;
            }

            bool canRemove = true;
            foreach (var node in nodes)
            {
                if (!node.Flags.Contains(DependencyTreeFlags.SupportsRemove))
                {
                    canRemove = false;
                    break;
                }

                var filePath = UnconfiguredProject.GetRelativePath(node.FilePath);
                if (string.IsNullOrEmpty(filePath))
                {
                    continue;
                }

                var dependency = snapshot.FindDependency(filePath, topLevel:true);
                if (dependency == null || dependency.Implicit)
                {
                    canRemove = false;
                    break;
                }
            }

            return canRemove;
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
                    node.Flags.Contains(DependencyTreeFlags.SharedProjectFlags));

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

                var snapshot = DependenciesSnapshotProvider.CurrentSnapshot;
                Requires.NotNull(snapshot, nameof(snapshot));
                if (snapshot == null)
                {
                    return;
                }

                // Handle the removal of shared import nodes.
                var projectXml = await access.GetProjectXmlAsync(UnconfiguredProject.FullPath)
                                             .ConfigureAwait(true);
                foreach (var sharedImportNode in sharedImportNodes)
                {
                    var sharedFilePath = UnconfiguredProject.GetRelativePath(sharedImportNode.FilePath);
                    if (string.IsNullOrEmpty(sharedFilePath))
                    {
                        continue;
                    }

                    var sharedProjectDependency = snapshot.FindDependency(sharedFilePath, topLevel:true);
                    if (sharedProjectDependency != null)
                    {
                        sharedFilePath = sharedProjectDependency.Path;
                    }

                    // Find the import that is included in the evaluation of the specified ConfiguredProject that
                    // imports the project file whose full path matches the specified one.
                    var matchingImports = from import in project.Imports
                                          where import.ImportingElement.ContainingProject == projectXml
                                          where PathHelper.IsSamePath(import.ImportedProject.FullPath, sharedFilePath)
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
            return ViewProviders.FirstOrDefault()?.Value.FindByPath(root, path);
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
                            DependenciesSnapshotProvider.SnapshotChanged += OnDependenciesSnapshotChanged;

                            Verify.NotDisposed(this);
                            var nowait = SubmitTreeUpdateAsync(
                                (treeSnapshot, configuredProjectExports, cancellationToken) =>
                                {
                                    var dependenciesNode = CreateDependenciesFolder(null);

                                    // TODO create providers nodes that can be visible when empty
                                    //dependenciesNode = CreateOrUpdateSubTreeProviderNodes(dependenciesNode, 
                                    //                                                      cancellationToken);

                                    return Task.FromResult(new TreeUpdateResult(dependenciesNode, true));
                                });
                        }

                    },
                    registerFaultHandler: true);
            }
        }

        private Task OnUnconfiguredProjectUnloading(object sender, EventArgs args)
        {
            UnconfiguredProject.ProjectUnloading -= OnUnconfiguredProjectUnloading;
            DependenciesSnapshotProvider.SnapshotChanged -= OnDependenciesSnapshotChanged;

            return Task.CompletedTask;
        }

        private void OnDependenciesSnapshotChanged(object sender, SnapshotChangedEventArgs e)
        {
            var snapshot = e.Snapshot;
            if (snapshot == null)
            {
                return;
            }

            lock (_treeUpdateLock)
            {
                if (_treeUpdateQueueTask == null || _treeUpdateQueueTask.IsCompleted)
                {
                    _treeUpdateQueueTask = ThreadingService.JoinableTaskFactory.RunAsync(async () =>
                    {
                        if (TasksService.UnloadCancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        await BuildTreeForSnapshotAsync(snapshot).ConfigureAwait(false);
                    }).Task;
                }
                else
                {
                    _treeUpdateQueueTask = _treeUpdateQueueTask.ContinueWith(
                        t => BuildTreeForSnapshotAsync(snapshot), TaskScheduler.Default);
                }
            }
        }

        private Task BuildTreeForSnapshotAsync(IDependenciesSnapshot snapshot)
        {
            var viewProvider = ViewProviders.FirstOrDefault();
            if (viewProvider == null)
            {
                return Task.CompletedTask;
            }

            var nowait = SubmitTreeUpdateAsync(
                async (treeSnapshot, configuredProjectExports, cancellationToken) =>
                {
                    var dependenciesNode = treeSnapshot.Value.Tree;
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        dependenciesNode = await viewProvider.Value.BuildTreeAsync(dependenciesNode, snapshot, cancellationToken)
                                                                   .ConfigureAwait(false);
                    }

                    // TODO We still are getting mismatched data sources and need to figure out better 
                    // way of merging, mute them for now and get to it in U1
                    return new TreeUpdateResult(dependenciesNode, false, null);
                });

            return Task.CompletedTask;
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
                    Icon = ManagedImageMonikers.ReferenceGroup.ToProjectSystemType(),
                    ExpandedIcon = ManagedImageMonikers.ReferenceGroup.ToProjectSystemType(),
                    Flags = DependencyTreeFlags.DependenciesRootNodeFlags
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

        private async Task<IImmutableDictionary<string, IPropertyPagesCatalog>> GetNamedCatalogsAsync(IProjectCatalogSnapshot catalogs)
        {
            if (catalogs != null)
            {
                return catalogs.NamedCatalogs;
            }

            if (NamedCatalogs != null)
            {
                return NamedCatalogs;
            }


            // Note: it is unlikely that we end up here, however for cases when node providers
            // getting their node data not from Design time build events, we might have OnDependenciesChanged
            // event coming before initial design time build event updates NamedCatalogs in this class.
            // Thus, just in case, explicitly request it here (GetCatalogsAsync will accuire a project read lock)
            NamedCatalogs = await ActiveConfiguredProject.Services
                                                         .PropertyPagesCatalog
                                                         .GetCatalogsAsync(CancellationToken.None)
                                                         .ConfigureAwait(false);

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
            var schemas = browseObjectCatalog.GetPropertyPagesSchemas(itemType);

            return from schemaName in browseObjectCatalog.GetPropertyPagesSchemas(itemType)
                   let schema = browseObjectCatalog.GetSchema(schemaName)
                   where schema.DataSource != null
                         && string.Equals(itemType, schema.DataSource.ItemType, StringComparison.OrdinalIgnoreCase)
                         && (resolved == string.Equals(schema.DataSource.Persistence,
                                                       RuleDataSourceTypes.PersistenceResolvedReference,
                                                       StringComparison.OrdinalIgnoreCase))
                   select schema;
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

        #region IDependencyTreeServices

        public IProjectTree CreateTree(
            string caption,
            IProjectPropertiesContext itemContext,
            IPropertySheet propertySheet = null,
            IRule browseObjectProperties = null,
            ProjectImageMoniker icon = null,
            ProjectImageMoniker expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = default(ProjectTreeFlags?))
        {
            return NewTree(
                caption: caption,
                item: itemContext,
                propertySheet: propertySheet,
                browseObjectProperties: browseObjectProperties,
                icon: icon,
                expandedIcon: expandedIcon,
                visible: visible,
                flags: flags);
        }

        public IProjectTree CreateTree(
            string caption,
            string filePath,
            IRule browseObjectProperties = null,
            ProjectImageMoniker icon = null,
            ProjectImageMoniker expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = default(ProjectTreeFlags?))
        {
            return NewTree(
                caption: caption,
                filePath: filePath,
                browseObjectProperties: browseObjectProperties,
                icon: icon,
                expandedIcon: expandedIcon,
                visible: visible,
                flags: flags);
        }

        public async Task<IRule> GetRuleAsync(IDependency dependency, IProjectCatalogSnapshot catalogs)
        {
            Requires.NotNull(dependency, nameof(dependency));

            ConfiguredProject project = null;
            if (dependency.TargetFramework.Equals(TargetFramework.Any))
            {
                project = ActiveConfiguredProject;
            }
            else
            {
                project = await DependenciesHost.GetConfiguredProject(dependency.TargetFramework)
                                                .ConfigureAwait(false) ?? ActiveConfiguredProject;
            }

            var configuredProjectExports = GetActiveConfiguredProjectExports(project);
            var namedCatalogs = await GetNamedCatalogsAsync(catalogs).ConfigureAwait(false);
            Requires.NotNull(namedCatalogs, nameof(namedCatalogs));

            var browseObjectsCatalog = namedCatalogs[PropertyPageContexts.BrowseObject];
            var schema = browseObjectsCatalog.GetSchema(dependency.SchemaName);
            var itemSpec = string.IsNullOrEmpty(dependency.OriginalItemSpec) ? dependency.Path : dependency.OriginalItemSpec;
            var context = ProjectPropertiesContext.GetContext(UnconfiguredProject,
                itemType: dependency.SchemaItemType,
                itemName: itemSpec);

            IRule rule = null;
            if (schema != null)
            {
                if (dependency.Resolved)
                {
                    rule = configuredProjectExports.RuleFactory.CreateResolvedReferencePageRule(
                                schema,
                                context,
                                dependency.Name,
                                dependency.Properties);
                }
                else
                {
                    rule = browseObjectsCatalog.BindToContext(schema.Name, context);
                }
            }
            else
            {
                // Since we have no browse object, we still need to create *something* so
                // that standard property pages can pop up.
                var emptyRule = RuleExtensions.SynthesizeEmptyRule(context.ItemType);
                return configuredProjectExports.PropertyPagesDataModelProvider.GetRule(
                            emptyRule,
                            context.File,
                            context.ItemType,
                            context.ItemName);
            }

            return rule;
        }

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
    }
}