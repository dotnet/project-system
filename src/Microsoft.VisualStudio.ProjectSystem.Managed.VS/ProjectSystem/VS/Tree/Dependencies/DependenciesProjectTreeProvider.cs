// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides the special "Dependencies" folder to project trees.
    /// </summary>
    [Export(ExportContractNames.ProjectTreeProviders.PhysicalViewRootGraft, typeof(IProjectTreeProvider))]
    [Export(typeof(IDependenciesTreeServices))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependenciesProjectTreeProvider
        : ProjectTreeProviderBase,
          IProjectTreeProvider,
          IDependenciesTreeServices
    {
        /// <summary><see cref="IProjectTreePropertiesProvider"/> imports that apply to the references tree.</summary>
        [ImportMany(ReferencesProjectTreeCustomizablePropertyValues.ContractName)]
        private readonly OrderPrecedenceImportCollection<IProjectTreePropertiesProvider> _projectTreePropertiesProviders;

        [ImportMany]
        private readonly OrderPrecedenceImportCollection<IDependenciesTreeViewProvider> _viewProviders;

        private readonly CancellationSeries _treeUpdateCancellationSeries = new CancellationSeries();
        private readonly ICrossTargetSubscriptionsHost _dependenciesHost;
        private readonly IDependenciesSnapshotProvider _dependenciesSnapshotProvider;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IProjectAccessor _projectAccessor;
        private readonly IDependencyTreeTelemetryService _treeTelemetryService;

        /// <summary>Latest updated snapshot of all rules schema catalogs.</summary>
        private IImmutableDictionary<string, IPropertyPagesCatalog> _namedCatalogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependenciesProjectTreeProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        public DependenciesProjectTreeProvider(
            IProjectThreadingService threadingService,
            IProjectAccessor projectAccessor,
            UnconfiguredProject unconfiguredProject,
            IDependenciesSnapshotProvider dependenciesSnapshotProvider,
            [Import(DependencySubscriptionsHost.DependencySubscriptionsHostContract)] ICrossTargetSubscriptionsHost dependenciesHost,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService tasksService,
            IDependencyTreeTelemetryService treeTelemetryService)
            : base(threadingService, unconfiguredProject)
        {
            _projectTreePropertiesProviders = new OrderPrecedenceImportCollection<IProjectTreePropertiesProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: unconfiguredProject);

            _viewProviders = new OrderPrecedenceImportCollection<IDependenciesTreeViewProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst,
                projectCapabilityCheckProvider: unconfiguredProject);

            _dependenciesSnapshotProvider = dependenciesSnapshotProvider;
            _dependenciesHost = dependenciesHost;
            _tasksService = tasksService;
            _projectAccessor = projectAccessor;
            _treeTelemetryService = treeTelemetryService;

            // Hook this so we can unregister the snapshot change event when the project unloads
            unconfiguredProject.ProjectUnloading += OnUnconfiguredProjectUnloading;

            Task OnUnconfiguredProjectUnloading(object sender, EventArgs args)
            {
                UnconfiguredProject.ProjectUnloading -= OnUnconfiguredProjectUnloading;
                _dependenciesSnapshotProvider.SnapshotChanged -= OnDependenciesSnapshotChanged;

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Gets the source block for the <see cref="IProjectTreeSnapshot" />.
        /// </summary>
        /// <remarks>
        /// This stub defined for code contracts.
        /// </remarks>
        IReceivableSourceBlock<IProjectVersionedValue<IProjectTreeSnapshot>> IProjectTreeProvider.Tree => Tree;

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

            IDependenciesSnapshot snapshot = _dependenciesSnapshotProvider.CurrentSnapshot;
            if (snapshot == null)
            {
                return false;
            }

            foreach (IProjectTree node in nodes)
            {
                if (!node.Flags.Contains(DependencyTreeFlags.SupportsRemove))
                {
                    return false;
                }

                string filePath = UnconfiguredProject.MakeRelative(node.FilePath);
                if (string.IsNullOrEmpty(filePath))
                {
                    continue;
                }

                IDependency dependency = snapshot.FindDependency(filePath, topLevel: true);
                if (dependency == null || dependency.Implicit)
                {
                    return false;
                }
            }

            return true;
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

            await _projectAccessor.OpenProjectForWriteAsync(ActiveConfiguredProject, project =>
            {
                // Handle the removal of normal reference Item Nodes (this excludes any shared import nodes).
                foreach (IProjectTree node in referenceItemNodes)
                {
                    if (node.BrowseObjectProperties?.Context == null)
                    {
                        // if node does not have an IRule with valid ProjectPropertiesContext we can not 
                        // get its itemsSpec. If nodes provided by custom IProjectDependenciesSubTreeProvider
                        // implementation, and have some custom IRule without context, it is not a problem,
                        // since they would not have DependencyNode.GenericDependencyFlags and we would not 
                        // end up here, since CanRemove would return false and Remove command would not show 
                        // up for those nodes. 
                        continue;
                    }

                    IProjectPropertiesContext nodeItemContext = node.BrowseObjectProperties.Context;
                    ProjectItem unresolvedReferenceItem = project.GetItemsByEvaluatedInclude(nodeItemContext.ItemName)
                        .FirstOrDefault(
                            (item, t) => string.Equals(item.ItemType, t, StringComparisons.ItemTypes), 
                            nodeItemContext.ItemType);

                    Report.IfNot(unresolvedReferenceItem != null, "Cannot find reference to remove.");
                    if (unresolvedReferenceItem != null)
                    {
                        project.RemoveItem(unresolvedReferenceItem);
                    }
                }

                IDependenciesSnapshot snapshot = _dependenciesSnapshotProvider.CurrentSnapshot;
                Requires.NotNull(snapshot, nameof(snapshot));
                if (snapshot == null)
                {
                    return;
                }

                // Handle the removal of shared import nodes.
                ProjectRootElement projectXml = project.Xml;
                foreach (IProjectTree sharedImportNode in sharedImportNodes)
                {
                    string sharedFilePath = UnconfiguredProject.MakeRelative(sharedImportNode.FilePath);
                    if (string.IsNullOrEmpty(sharedFilePath))
                    {
                        continue;
                    }

                    IDependency sharedProjectDependency = snapshot.FindDependency(sharedFilePath, topLevel: true);
                    if (sharedProjectDependency != null)
                    {
                        sharedFilePath = sharedProjectDependency.Path;
                    }

                    // Find the import that is included in the evaluation of the specified ConfiguredProject that
                    // imports the project file whose full path matches the specified one.
                    IEnumerable<ResolvedImport> matchingImports = from import in project.Imports
                                                                  where import.ImportingElement.ContainingProject == projectXml
                                                                     && PathHelper.IsSamePath(import.ImportedProject.FullPath, sharedFilePath)
                                                                  select import;
                    foreach (ResolvedImport importToRemove in matchingImports)
                    {
                        ProjectImportElement importingElementToRemove = importToRemove.ImportingElement;
                        Report.IfNot(importingElementToRemove != null,
                                     "Cannot find shared project reference to remove.");
                        if (importingElementToRemove != null)
                        {
                            importingElementToRemove.Parent.RemoveChild(importingElementToRemove);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Efficiently finds a descendent with the given path in the given tree.
        /// </summary>
        /// <param name="root">The root of the tree.</param>
        /// <param name="path">The absolute or project-relative path to the item sought.</param>
        /// <returns>The item in the tree if found; otherwise <c>null</c>.</returns>
        public override IProjectTree FindByPath(IProjectTree root, string path)
        {
            // We override this since we need to find children under either:
            //
            // - our dependencies root node
            // - dependency sub tree nodes
            // - dependency sub tree top level nodes
            //
            // Deeper levels will be graph nodes with additional info, not direct dependencies
            // specified in the project file.

            return _viewProviders.FirstOrDefault()?.Value.FindByPath(root, path);
        }

        /// <summary>
        /// Gets the path to a given node that can later be provided to <see cref="IProjectTreeProvider.FindByPath" /> to locate the node again.
        /// </summary>
        /// <param name="node">The node whose path is sought.</param>
        /// <returns>
        /// A non-empty string, or <c>null</c> if searching is not supported.
        /// For nodes that represent files on disk, this is the project-relative path to that file.
        /// The root node of a project is the absolute path to the project file.
        /// </returns>
        public override string GetPath(IProjectTree node)
        {
            // Needed for graph nodes search
            return node.FilePath;
        }

        /// <summary>
        /// Generates the original references directory tree.
        /// </summary>
        protected override void Initialize()
        {
#pragma warning disable RS0030 // symbol LoadedProject is banned
            using (UnconfiguredProjectAsynchronousTasksService.LoadedProject())
#pragma warning restore RS0030 // symbol LoadedProject is banned
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
                            Verify.NotDisposed(this);

                            // Issue this token before hooking the SnapshotChanged event to prevent a race
                            // where a snapshot tree is replaced by the initial, empty tree created below.
                            // The handler will cancel this token before submitting its update.
                            CancellationToken initialTreeCancellationToken = _treeUpdateCancellationSeries.CreateNext();

                            _dependenciesSnapshotProvider.SnapshotChanged += OnDependenciesSnapshotChanged;

                            Task<IProjectVersionedValue<IProjectTreeSnapshot>> nowait = SubmitTreeUpdateAsync(
                                (treeSnapshot, configuredProjectExports, cancellationToken) =>
                                {
                                    IProjectTree dependenciesNode = CreateDependenciesFolder();

                                    // TODO create providers nodes that can be visible when empty
                                    //dependenciesNode = CreateOrUpdateSubTreeProviderNodes(dependenciesNode, 
                                    //                                                      cancellationToken);

                                    return Task.FromResult(new TreeUpdateResult(dependenciesNode));
                                },
                                initialTreeCancellationToken);
                        }

                    },
                    registerFaultHandler: true);
            }

            IProjectTree CreateDependenciesFolder()
            {
                var values = new ReferencesProjectTreeCustomizablePropertyValues
                {
                    Caption = VSResources.DependenciesNodeName,
                    Icon = ManagedImageMonikers.ReferenceGroup.ToProjectSystemType(),
                    ExpandedIcon = ManagedImageMonikers.ReferenceGroup.ToProjectSystemType(),
                    Flags = DependencyTreeFlags.DependenciesRootNodeFlags
                };

                // Allow property providers to perform customization.
                // These are ordered from lowest priority to highest, allowing higher priority
                // providers to override lower priority providers.
                foreach (IProjectTreePropertiesProvider provider in _projectTreePropertiesProviders.ExtensionValues())
                {
                    provider.CalculatePropertyValues(null, values);
                }

                // Note that all the parameters are specified so we can force this call to an
                // overload of NewTree available prior to 15.5 versions of CPS. Once a 15.5 build
                // is publicly available we can move this to an overload with default values for
                // most of the parameters, and we'll only need to pass the interesting ones.
                return NewTree(
                    caption: values.Caption,
                    filePath: null,
                    browseObjectProperties: null,
                    icon: values.Icon,
                    expandedIcon: values.ExpandedIcon,
                    visible: true,
                    flags: values.Flags);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _treeUpdateCancellationSeries.Dispose();
            }

            base.Dispose(disposing);
        }

        private void OnDependenciesSnapshotChanged(object sender, SnapshotChangedEventArgs e)
        {
            IDependenciesSnapshot snapshot = e.Snapshot;

            if (snapshot == null)
            {
                return;
            }

            if (_tasksService.UnloadCancellationToken.IsCancellationRequested || e.Token.IsCancellationRequested)
            {
                return;
            }

            // Take the highest priority view provider
            IDependenciesTreeViewProvider viewProvider = _viewProviders.FirstOrDefault()?.Value;

            if (viewProvider == null)
            {
                return;
            }

            _ = SubmitTreeUpdateAsync(
                async (treeSnapshot, configuredProjectExports, cancellationToken) =>
                {
                    IProjectTree dependenciesNode = treeSnapshot.Value.Tree;
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        dependenciesNode = await viewProvider.BuildTreeAsync(dependenciesNode, snapshot, cancellationToken);

                        await _treeTelemetryService.ObserveTreeUpdateCompletedAsync(snapshot.HasUnresolvedDependency);
                    }

                    // TODO We still are getting mismatched data sources and need to figure out better 
                    // way of merging, mute them for now and get to it in U1
                    return new TreeUpdateResult(dependenciesNode);
                },
                _treeUpdateCancellationSeries.CreateNext(e.Token));
        }

        /// <summary>
        /// Creates a new instance of the configured project exports class.
        /// </summary>
        protected override ConfiguredProjectExports GetActiveConfiguredProjectExports(ConfiguredProject newActiveConfiguredProject)
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
            ProjectTreeFlags? flags = default)
        {
            // Note that all the parameters are specified so we can force this call to an
            // overload of NewTree available prior to 15.5 versions of CPS. Once a 15.5 build
            // is publicly available we can move this to an overload with default values for
            // most of the parameters, and we'll only need to pass the interesting ones.
            return NewTree(
                caption: caption,
                item: itemContext,
                propertySheet: propertySheet,
                browseObjectProperties: browseObjectProperties,
                icon: icon,
                expandedIcon: expandedIcon,
                visible: visible,
                flags: flags,
                isLinked: false);
        }

        public IProjectTree CreateTree(
            string caption,
            string filePath,
            IRule browseObjectProperties = null,
            ProjectImageMoniker icon = null,
            ProjectImageMoniker expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = default)
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

            ConfiguredProject project = dependency.TargetFramework.Equals(TargetFramework.Any)
                ? ActiveConfiguredProject
                : await _dependenciesHost.GetConfiguredProject(dependency.TargetFramework) ?? ActiveConfiguredProject;

            IImmutableDictionary<string, IPropertyPagesCatalog> namedCatalogs = await GetNamedCatalogsAsync();
            Requires.NotNull(namedCatalogs, nameof(namedCatalogs));

            IPropertyPagesCatalog browseObjectsCatalog = namedCatalogs[PropertyPageContexts.BrowseObject];
            Rule schema = browseObjectsCatalog.GetSchema(dependency.SchemaName);
            string itemSpec = string.IsNullOrEmpty(dependency.OriginalItemSpec) ? dependency.Path : dependency.OriginalItemSpec;
            var context = ProjectPropertiesContext.GetContext(UnconfiguredProject,
                itemType: dependency.SchemaItemType,
                itemName: itemSpec);

            if (schema == null)
            {
                // Since we have no browse object, we still need to create *something* so
                // that standard property pages can pop up.
                Rule emptyRule = RuleExtensions.SynthesizeEmptyRule(context.ItemType);
                return GetActiveConfiguredProjectExports(project).PropertyPagesDataModelProvider.GetRule(
                    emptyRule,
                    context.File,
                    context.ItemType,
                    context.ItemName);
            }

            if (dependency.Resolved)
            {
                return GetActiveConfiguredProjectExports(project).RuleFactory.CreateResolvedReferencePageRule(
                    schema,
                    context,
                    dependency.Name,
                    dependency.Properties);
            }

            return browseObjectsCatalog.BindToContext(schema.Name, context);

            async Task<IImmutableDictionary<string, IPropertyPagesCatalog>> GetNamedCatalogsAsync()
            {
                if (catalogs != null)
                {
                    return catalogs.NamedCatalogs;
                }

                if (_namedCatalogs == null)
                {
                    // Note: it is unlikely that we end up here, however for cases when node providers
                    // getting their node data not from Design time build events, we might have OnDependenciesChanged
                    // event coming before initial design time build event updates NamedCatalogs in this class.
                    // Thus, just in case, explicitly request it here (GetCatalogsAsync will acquire a project read lock)
                    _namedCatalogs = await ActiveConfiguredProject.Services
                        .PropertyPagesCatalog
                        .GetCatalogsAsync(CancellationToken.None);
                }

                return _namedCatalogs;
            }
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
