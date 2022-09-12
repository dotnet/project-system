// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    /// <summary>
    /// Provides the special "Dependencies" folder to project trees.
    /// </summary>
    /// <remarks>
    /// This provider handles data subscription. It delegates the construction of the actual "Dependencies" node tree
    /// to an instance of <see cref="IDependenciesTreeViewProvider" />.
    /// </remarks>
    [Export(ExportContractNames.ProjectTreeProviders.PhysicalViewRootGraft, typeof(IProjectTreeProvider))]
    [Export(typeof(IDependenciesTreeServices))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependenciesProjectTreeProvider
        : ProjectTreeProviderBase,
          IDependenciesTreeServices
    {
        /// <summary><see cref="IProjectTreePropertiesProvider"/> imports that apply to the references tree.</summary>
        [ImportMany(ReferencesProjectTreeCustomizablePropertyValues.ContractName)]
        private readonly OrderPrecedenceImportCollection<IProjectTreePropertiesProvider> _projectTreePropertiesProviders;

        [ImportMany("DependencyTreeRemovalActionHandlers")]
        private readonly OrderPrecedenceImportCollection<IProjectTreeActionHandler> _removalActionHandlers;

        [ImportMany]
        private readonly OrderPrecedenceImportCollection<IDependenciesTreeViewProvider> _viewProviders;

        private readonly CancellationSeries _treeUpdateCancellationSeries = new();
        private readonly DependenciesSnapshotProvider _dependenciesSnapshotProvider;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly UnconfiguredProject _project;
        private readonly IProjectAccessor _projectAccessor;
        private readonly IDependencyTreeTelemetryService _treeTelemetryService;

        /// <summary>Latest updated snapshot of all rules schema catalogs.</summary>
        private IImmutableDictionary<string, IPropertyPagesCatalog>? _namedCatalogs;

        /// <summary>
        /// A subscription to the snapshot service dataflow.
        /// </summary>
        private IDisposable? _snapshotEventListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependenciesProjectTreeProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        public DependenciesProjectTreeProvider(
            IProjectThreadingService threadingService,
            UnconfiguredProject project,
            IProjectAccessor projectAccessor,
            UnconfiguredProject unconfiguredProject,
            DependenciesSnapshotProvider dependenciesSnapshotProvider,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService tasksService,
            IDependencyTreeTelemetryService treeTelemetryService)
            : base(threadingService, unconfiguredProject)
        {
            _projectTreePropertiesProviders = new OrderPrecedenceImportCollection<IProjectTreePropertiesProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: unconfiguredProject);

            _removalActionHandlers = new OrderPrecedenceImportCollection<IProjectTreeActionHandler>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst,
                projectCapabilityCheckProvider: unconfiguredProject);

            _viewProviders = new OrderPrecedenceImportCollection<IDependenciesTreeViewProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst,
                projectCapabilityCheckProvider: unconfiguredProject);

            _dependenciesSnapshotProvider = dependenciesSnapshotProvider;
            _tasksService = tasksService;
            _project = project;
            _projectAccessor = projectAccessor;
            _treeTelemetryService = treeTelemetryService;

            // Hook this so we can unregister the snapshot change event when the project unloads
            unconfiguredProject.ProjectUnloading += OnUnconfiguredProjectUnloading;

            Task OnUnconfiguredProjectUnloading(object? sender, EventArgs args)
            {
                UnconfiguredProject.ProjectUnloading -= OnUnconfiguredProjectUnloading;
                _snapshotEventListener?.Dispose();

                return Task.CompletedTask;
            }
        }

        public override string? GetPath(IProjectTree node)
        {
            // If the node's FilePath is null, we are going to return null regardless of whether
            // this node belongs to the dependencies tree or not, so avoid extra work by retuning
            // immediately here.
            if (node.FilePath is null)
            {
                return null;
            }

            // Walk up from node through all its ancestors.
            for (IProjectTree? step = node; step is not null; step = step.Parent)
            {
                if (step.Flags.Contains(DependencyTreeFlags.DependenciesRootNode))
                {
                    // This node is contained within the Dependencies tree.
                    //
                    // node.FilePath can be null. Some dependency types (e.g. packages) do not require a file path,
                    // while other dependency types (e.g. analyzers) do.
                    //
                    // Returning null from a root graft causes CPS to use the "pseudo path" for the item, which has
                    // form ">123" where the number is the item's identity. This is a short string (low memory overhead)
                    // and allows fast lookup. So in general we want to return null here unless there is a compelling
                    // requirement to use the path.

                    return node.FilePath;
                }
            }

            return null;
        }

        public override IProjectTree? FindByPath(IProjectTree root, string path)
        {
            // We are _usually_ passed the project root here, and we know that our tree items are limited to the
            // "Dependencies" subtree, so scope the search to that node.
            //
            // If we are passed a root which is not the project node, we will not find any search results.
            // This does not appear to be an issue, but may one day be required.
            IProjectTree? dependenciesRootNode = root.FindChildWithFlags(DependencyTreeFlags.DependenciesRootNode);

            return dependenciesRootNode?.GetSelfAndDescendentsDepthFirst().FirstOrDefault((node, p) => StringComparers.Paths.Equals(node.FilePath, p), path);
        }

        public override bool CanRemove(IImmutableSet<IProjectTree> nodes, DeleteOptions deleteOptions = DeleteOptions.None)
        {
            if (deleteOptions.HasFlag(DeleteOptions.DeleteFromStorage))
            {
                // We support "Remove" but not "Delete".
                // We remove the dependency from the project, not delete it from disk.
                return false;
            }

            if (_removalActionHandlers.Count != 0)
            {
                var context = new ProjectDependencyTreeRemovalActionHandlerContext(this);

                foreach (IProjectTreeActionHandler handler in _removalActionHandlers.ExtensionValues())
                {
                    if (!handler.CanRemove(context, nodes, deleteOptions))
                    {
                        return false;
                    }
                }
            }

            return nodes.All(node => node.Flags.Contains(DependencyTreeFlags.SupportsRemove));
        }

        /// <inheritdoc />
        /// <remarks>
        /// Delete and Remove commands are handled via IVsHierarchyDeleteHandler3, not by
        /// IAsyncCommandGroupHandler and first asks us we CanRemove nodes. If yes then RemoveAsync is called.
        /// We can remove only nodes that are standard and based on project items, i.e. nodes that
        /// are created by default IProjectDependenciesSubTreeProvider implementations and have
        /// DependencyNode.GenericDependencyFlags flags and IRule with Context != null, in order to obtain
        /// node's itemSpec. ItemSpec then used to remove a project item having same Include.
        /// </remarks>
        public override async Task RemoveAsync(IImmutableSet<IProjectTree> nodes, DeleteOptions deleteOptions = DeleteOptions.None)
        {
            if (!CanRemove(nodes, deleteOptions))
            {
                throw new InvalidOperationException();
            }

            // Get the list of shared import nodes.
            IEnumerable<IProjectTree> sharedImportNodes = nodes.Where(node =>
                    node.Flags.Contains(DependencyTreeFlags.SharedProjectDependency));

            // Get the list of normal reference Item Nodes (this excludes any shared import nodes).
            IEnumerable<IProjectTree> referenceItemNodes = nodes.Except(sharedImportNodes);

            Assumes.NotNull(ActiveConfiguredProject);

            await _projectAccessor.OpenProjectForWriteAsync(ActiveConfiguredProject, project =>
            {
                // Handle the removal of normal reference Item Nodes (this excludes any shared import nodes).
                foreach (IProjectTree node in referenceItemNodes)
                {
                    if (node.BrowseObjectProperties?.Context is null)
                    {
                        // If node does not have an IRule with valid ProjectPropertiesContext we can not
                        // get its itemsSpec. If nodes provided by custom IProjectDependenciesSubTreeProvider
                        // implementation, and have some custom IRule without context, it is not a problem,
                        // since they would not have DependencyNode.GenericDependencyFlags and we would not
                        // end up here, since CanRemove would return false and Remove command would not show
                        // up for those nodes.
                        continue;
                    }

                    IProjectPropertiesContext nodeItemContext = node.BrowseObjectProperties.Context;
                    ProjectItem? unresolvedReferenceItem = project.GetItemsByEvaluatedInclude(nodeItemContext.ItemName)
                        .FirstOrDefault(
                            (item, t) => string.Equals(item.ItemType, t, StringComparisons.ItemTypes),
                            nodeItemContext.ItemType);

                    Report.IfNot(unresolvedReferenceItem is not null, "Cannot find reference to remove.");
                    if (unresolvedReferenceItem is not null)
                    {
                        project.RemoveItem(unresolvedReferenceItem);
                    }
                }

                // Handle the removal of shared import nodes.
                ProjectRootElement projectXml = project.Xml;
                foreach (IProjectTree sharedImportNode in sharedImportNodes)
                {
                    string? sharedFilePath = sharedImportNode.FilePath;
                    if (Strings.IsNullOrEmpty(sharedFilePath))
                    {
                        continue;
                    }

                    // Find the import that is included in the evaluation of the specified ConfiguredProject that
                    // imports the project file whose full path matches the specified one.
                    foreach (ResolvedImport import in project.Imports)
                    {
                        if (import.ImportingElement.ContainingProject != projectXml || !PathHelper.IsSamePath(import.ImportedProject.FullPath, sharedFilePath))
                        {
                            // This is not the import we are trying to remove.
                            continue;
                        }

                        ProjectImportElement importingElementToRemove = import.ImportingElement;

                        if (importingElementToRemove is null)
                        {
                            Report.Fail("Cannot find shared project reference to remove.");
                            continue;
                        }

                        // We found a matching import. Remove it.
                        importingElementToRemove.Parent.RemoveChild(importingElementToRemove);

                        // Stop scanning for imports.
                        break;
                    }
                }
            });

            if (_removalActionHandlers.Count != 0)
            {
                var context = new ProjectDependencyTreeRemovalActionHandlerContext(this);

                foreach (IProjectTreeActionHandler handler in _removalActionHandlers.ExtensionValues())
                {
                    await handler.RemoveAsync(context, nodes, deleteOptions);
                }
            }
        }

        /// <summary>
        /// Generates the original references directory tree.
        /// </summary>
        protected override void Initialize()
        {
#pragma warning disable RS0030 // symbol LoadedProject is banned
            using (UnconfiguredProjectAsynchronousTasksService.LoadedProject())
#pragma warning restore RS0030
            {
#pragma warning disable RS0030 // https://github.com/dotnet/roslyn-analyzers/issues/3295
                base.Initialize();
#pragma warning restore RS0030

                // this.IsApplicable may take a project lock, so we can't do it inline with this method
                // which is holding a private lock.  It turns out that doing it asynchronously isn't a problem anyway,
                // so long as we guard against races with the Dispose method.
#pragma warning disable RS0030 // symbol LoadedProjectAsync is banned
                JoinableTask task = UnconfiguredProjectAsynchronousTasksService.LoadedProjectAsync(
#pragma warning restore RS0030
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

                            _ = SubmitTreeUpdateAsync(
                                delegate
                                {
                                    IProjectTree dependenciesNode = CreateDependenciesNode();

                                    return Task.FromResult(new TreeUpdateResult(dependenciesNode));
                                },
                                initialTreeCancellationToken);

                            ITargetBlock<SnapshotChangedEventArgs> actionBlock = DataflowBlockFactory.CreateActionBlock<SnapshotChangedEventArgs>(
                                OnDependenciesSnapshotChangedAsync,
                                _project,
                                nameFormat: "DependenciesProjectTreeProviderSource {1}",
                                skipIntermediateInputData: true);
                            _snapshotEventListener = _dependenciesSnapshotProvider.SnapshotChangedSource.LinkTo(actionBlock, DataflowOption.PropagateCompletion);
                        }
                    },
                    registerFaultHandler: true);

                _project.Services.FaultHandler.Forget(task.Task, _project);
            }

            IProjectTree CreateDependenciesNode()
            {
                var values = new ReferencesProjectTreeCustomizablePropertyValues
                {
                    Caption = Resources.DependenciesNodeName,
                    Icon = KnownMonikers.ReferenceGroup.ToProjectSystemType(),
                    ExpandedIcon = KnownMonikers.ReferenceGroup.ToProjectSystemType(),
                    Flags = ProjectTreeFlags.Create(ProjectTreeFlags.Common.BubbleUp)
                          + ProjectTreeFlags.Create(ProjectTreeFlags.Common.ReferencesFolder)
                          + ProjectTreeFlags.Create(ProjectTreeFlags.Common.VirtualFolder)
                          + DependencyTreeFlags.DependenciesRootNode
                };

                // Allow property providers to perform customization.
                // These are ordered from lowest priority to highest, allowing higher priority
                // providers to override lower priority providers.
                foreach (IProjectTreePropertiesProvider provider in _projectTreePropertiesProviders.ExtensionValues())
                {
                    provider.CalculatePropertyValues(ProjectTreeCustomizablePropertyContext.Instance, values);
                }

                return NewTree(
                    caption: values.Caption,
                    icon: values.Icon,
                    expandedIcon: values.ExpandedIcon,
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

        private async Task OnDependenciesSnapshotChangedAsync(SnapshotChangedEventArgs e)
        {
            DependenciesSnapshot snapshot = e.Snapshot;

            if (_tasksService.UnloadCancellationToken.IsCancellationRequested || e.Token.IsCancellationRequested)
            {
                return;
            }

            // Take the highest priority view provider
            IDependenciesTreeViewProvider? viewProvider = _viewProviders.FirstOrDefault()?.Value;

            if (viewProvider is null)
            {
                return;
            }

            try
            {
                await SubmitTreeUpdateAsync(UpdateTreeAsync, _treeUpdateCancellationSeries.CreateNext(e.Token));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                // We do not expect any exception when we call SubmitTreeUpdateAsync, but we don't want to leak an exception here.
                // Because it will fault the dataflow block which stops further tree updates.
                _ = ProjectFaultHandlerService.ReportFaultAsync(ex, UnconfiguredProject);
            }

            return;

            async Task<TreeUpdateResult> UpdateTreeAsync(
                IProjectVersionedValue<IProjectTreeSnapshot>? treeSnapshot,
                ConfiguredProjectExports? configuredProjectExports,
                CancellationToken cancellationToken)
            {
                Assumes.NotNull(treeSnapshot); // we specify the initial tree so it will not be null here

                IProjectTree dependenciesNode = treeSnapshot.Value.Tree;

                if (!cancellationToken.IsCancellationRequested)
                {
                    dependenciesNode = await viewProvider.BuildTreeAsync(dependenciesNode, snapshot, cancellationToken);

                    await _treeTelemetryService.ObserveTreeUpdateCompletedAsync(snapshot.MaximumVisibleDiagnosticLevel != DiagnosticLevel.None);
                }

                return new TreeUpdateResult(dependenciesNode);
            }
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
            IPropertySheet? propertySheet = null,
            IRule? browseObjectProperties = null,
            ProjectImageMoniker? icon = null,
            ProjectImageMoniker? expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = null)
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
            string? filePath,
            IRule? browseObjectProperties = null,
            ProjectImageMoniker? icon = null,
            ProjectImageMoniker? expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = null)
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

        public async Task<IRule?> GetBrowseObjectRuleAsync(IDependency dependency, TargetFramework targetFramework, IProjectCatalogSnapshot? catalogs)
        {
            Requires.NotNull(dependency, nameof(dependency));

            IImmutableDictionary<string, IPropertyPagesCatalog> namedCatalogs = await GetNamedCatalogsAsync();

            Requires.NotNull(namedCatalogs, nameof(namedCatalogs));

            if (!namedCatalogs.TryGetValue(PropertyPageContexts.BrowseObject, out IPropertyPagesCatalog browseObjectsCatalog))
            {
                // Issue https://github.com/dotnet/project-system/issues/4860 suggests this code path
                // can exist, however a repro was not found to dig deeper into the underlying cause.
                // For now just return null as the upstream caller handles null correctly anyway.
                return null;
            }

            string? itemSpec = string.IsNullOrEmpty(dependency.OriginalItemSpec)
                ? dependency.FilePath
                : dependency.OriginalItemSpec;

            var context = ProjectPropertiesContext.GetContext(
                UnconfiguredProject,
                itemType: dependency.SchemaItemType,
                itemName: itemSpec);

            Rule? schema = dependency.SchemaName is not null ? browseObjectsCatalog.GetSchema(dependency.SchemaName) : null;

            if (schema is null)
            {
                // Since we have no browse object, we still need to create *something* so
                // that standard property pages can pop up.
                Rule emptyRule = RuleExtensions.SynthesizeEmptyRule(context.ItemType);

                return GetConfiguredProjectExports().PropertyPagesDataModelProvider.GetRule(
                    emptyRule,
                    context.File,
                    context.ItemType,
                    context.ItemName);
            }

            if (dependency.Resolved && !Strings.IsNullOrEmpty(dependency.OriginalItemSpec))
            {
                return GetConfiguredProjectExports().RuleFactory.CreateResolvedReferencePageRule(
                    schema,
                    context,
                    dependency.OriginalItemSpec,
                    dependency.BrowseObjectProperties);
            }

            return browseObjectsCatalog.BindToContext(schema.Name, context);

            async Task<IImmutableDictionary<string, IPropertyPagesCatalog>> GetNamedCatalogsAsync()
            {
                if (catalogs is not null)
                {
                    return catalogs.NamedCatalogs;
                }

                if (_namedCatalogs is null)
                {
                    Assumes.NotNull(ActiveConfiguredProject);
                    Assumes.Present(ActiveConfiguredProject.Services.PropertyPagesCatalog);

                    // Note: it is unlikely that we end up here, however for cases when node providers
                    // getting their node data not from Design time build events, we might have OnDependenciesChanged
                    // event coming before initial design time build event updates NamedCatalogs in this class.
                    // Thus, just in case, explicitly request it here (GetCatalogsAsync will acquire a project read lock)
                    _namedCatalogs = await ActiveConfiguredProject.Services.PropertyPagesCatalog.GetCatalogsAsync();
                }

                return _namedCatalogs;
            }

            ConfiguredProjectExports GetConfiguredProjectExports()
            {
                Assumes.NotNull(ActiveConfiguredProject);

                ConfiguredProject project = targetFramework.Equals(TargetFramework.Any)
                    ? ActiveConfiguredProject
                    : _dependenciesSnapshotProvider.GetConfiguredProject(targetFramework) ?? ActiveConfiguredProject;

                return GetActiveConfiguredProjectExports(project);
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

        /// <summary>
        /// A private implementation of <see cref="IProjectTreeCustomizablePropertyContext"/> used when creating
        /// the dependencies nodes.
        /// </summary>
        private sealed class ProjectTreeCustomizablePropertyContext : IProjectTreeCustomizablePropertyContext
        {
            public static readonly ProjectTreeCustomizablePropertyContext Instance = new();

            public string ItemName => string.Empty;
            public string? ItemType => null;
            public IImmutableDictionary<string, string> Metadata => ImmutableDictionary<string, string>.Empty;
            public ProjectTreeFlags ParentNodeFlags => ProjectTreeFlags.Empty;
            public bool ExistsOnDisk => false;
            public bool IsFolder => false;
            public bool IsNonFileSystemProjectItem => true;
            public IImmutableDictionary<string, string> ProjectTreeSettings => ImmutableDictionary<string, string>.Empty;
        }

        /// <summary>
        /// A private implementation of <see cref="IProjectTreeActionHandlerContext"/> for use with
        /// <see cref="IProjectTreeActionHandler"/> exports.
        /// </summary>
        private sealed class ProjectDependencyTreeRemovalActionHandlerContext : IProjectTreeActionHandlerContext
        {
            public IProjectTreeProvider TreeProvider { get; }

            public IProjectTreeActionHandler SuccessorHandlerDelegator => null!;

            public ProjectDependencyTreeRemovalActionHandlerContext(IProjectTreeProvider treeProvider)
            {
                TreeProvider = treeProvider;
            }
        }
    }
}
