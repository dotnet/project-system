// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;

/// <summary>
/// Provides the "Dependencies" folder to project trees, and populates the project's top-level dependencies beneath it.
/// </summary>
/// <remarks>
/// <para>
/// This class is a "root graft tree provider", which CPS uses to attach a subtree of the project node in Solution Explorer.
/// </para>
/// <para>
/// It initiates a data subscription to receive <see cref="DependenciesSnapshot"/> updates via <see cref="DependenciesSnapshotProvider"/>.
/// It uses <see cref="DependenciesTreeBuilder"/> to construct and synchronize the <see cref="IProjectTree"/> that reflects
/// the displayed tree.
/// </para>
/// <para>
/// This class is maps between paths and tree items, via <see cref="GetPath"/> and <see cref="FindByPath"/>.
/// </para>
/// <para>
/// This class controls and performs removal of dependencies, via <see cref="CanRemove"/> and <see cref="RemoveAsync"/>.
/// </para>
/// </remarks>
[Export(typeof(IProjectTreeOperations))]
[Export(ExportContractNames.ProjectTreeProviders.PhysicalViewRootGraft, typeof(IProjectTreeProvider))]
[AppliesTo(ProjectCapability.DependenciesTree)]
internal sealed partial class DependenciesTreeProvider : ProjectTreeProviderBase, IProjectTreeOperations
{
    /// <summary>
    /// Import extensions that control how dependency removal is handled.
    /// </summary>
    /// <remarks>
    /// Used by WapProj, for example.
    /// </remarks>
    [ImportMany("DependencyTreeRemovalActionHandlers")]
    private readonly OrderPrecedenceImportCollection<IProjectTreeActionHandler> _removalActionHandlers;

    private readonly DependenciesSnapshotProvider _dependenciesSnapshotProvider;
    private readonly CancellationSeries _treeUpdateCancellationSeries;
    private readonly IProjectAccessor _projectAccessor;
    private readonly ITaskDelayScheduler _debounce;

    [Import]
    private DependenciesTreeBuilder TreeBuilder { get; set; } = null!;

    /// <summary>Latest updated snapshot of all rules schema catalogs.</summary>
    private IImmutableDictionary<string, IPropertyPagesCatalog>? _namedCatalogs;

    /// <summary>
    /// A subscription to the snapshot service dataflow.
    /// </summary>
    private IDisposable? _snapshotEventListener;

    [ImportingConstructor]
    public DependenciesTreeProvider(
        UnconfiguredProject unconfiguredProject,
        IProjectThreadingService threadingService,
        IUnconfiguredProjectTasksService tasksService,
        IProjectAccessor projectAccessor,
        DependenciesSnapshotProvider dependenciesSnapshotProvider)
        : base(threadingService, unconfiguredProject)
    {
        _dependenciesSnapshotProvider = dependenciesSnapshotProvider;
        _projectAccessor = projectAccessor;

        _removalActionHandlers = new OrderPrecedenceImportCollection<IProjectTreeActionHandler>(
            ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst,
            projectCapabilityCheckProvider: unconfiguredProject);

        _treeUpdateCancellationSeries = new(tasksService.UnloadCancellationToken);

        // Updates are debounced to conflate rapid updates and reduce frequency of tree updates downstream.
        _debounce = new TaskDelayScheduler(
            TimeSpan.FromMilliseconds(250),
            threadingService,
            tasksService.UnloadCancellationToken);
    }

    /// <summary>
    /// Generates the original references directory tree.
    /// </summary>
    protected override void Initialize()
    {
        CancellationToken token = UnconfiguredProjectAsynchronousTasksService.UnloadCancellationToken;

        DependenciesSnapshot? snapshot = null;

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

                    token.ThrowIfCancellationRequested();

                    await _dependenciesSnapshotProvider.EnsureInitializedAsync();

                    lock (SyncObject)
                    {
                        Verify.NotDisposed(this);

                        ITargetBlock<IProjectVersionedValue<DependenciesSnapshot>> actionBlock = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<DependenciesSnapshot>>(
                            OnDependenciesSnapshotChangedAsync,
                            UnconfiguredProject,
                            nameFormat: "Dependencies snapshot action {1}",
                            skipIntermediateInputData: true);
                        _snapshotEventListener = _dependenciesSnapshotProvider.Source.LinkTo(actionBlock, DataflowOption.PropagateCompletion);
                    }
                },
                registerFaultHandler: true);

            UnconfiguredProject.Services.FaultHandler.Forget(task.Task, UnconfiguredProject);
        }

        return;

        Task OnDependenciesSnapshotChangedAsync(IProjectVersionedValue<DependenciesSnapshot> update)
        {
            if (UnconfiguredProjectAsynchronousTasksService.UnloadCancellationToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            if (ReferenceEquals(update.Value, snapshot))
            {
                return Task.CompletedTask;
            }

            snapshot = update.Value;

            // Conflate rapid snapshot updates by debouncing events over a short window.
            // This reduces the frequency of tree updates with minimal perceived latency.
            // TODO is this asynchrony the reason that the tree does not appear populated when the project loads? https://github.com/dotnet/project-system/issues/8949
            _ = _debounce.ScheduleAsyncTask(
                async cancellationToken =>
                {
                    if (cancellationToken.IsCancellationRequested || IsDisposed)
                    {
                        return;
                    }

                    try
                    {
                        await SubmitTreeUpdateAsync(UpdateTreeAsync, _treeUpdateCancellationSeries.CreateNext(cancellationToken));
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
                },
                UnconfiguredProjectAsynchronousTasksService.UnloadCancellationToken);

            return Task.CompletedTask;

            async Task<TreeUpdateResult> UpdateTreeAsync(
                IProjectVersionedValue<IProjectTreeSnapshot>? treeSnapshot,
                ConfiguredProjectExports? configuredProjectExports,
                CancellationToken cancellationToken)
            {
                IProjectTree dependenciesNode = await TreeBuilder.BuildTreeAsync(treeSnapshot?.Value.Tree, snapshot, cancellationToken);

                return new TreeUpdateResult(dependenciesNode);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _snapshotEventListener?.Dispose();
            _treeUpdateCancellationSeries.Dispose();
            _debounce.Dispose();
        }

        base.Dispose(disposing);
    }

    public override string? GetPath(IProjectTree node)
    {
        // If the node's FilePath is null, we are going to return null regardless of whether
        // this node belongs to the dependencies tree or not, so avoid extra work by returning
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

    protected override ConfiguredProjectExports GetActiveConfiguredProjectExports(ConfiguredProject newActiveConfiguredProject)
    {
        return GetActiveConfiguredProjectExports<MyConfiguredProjectExports>(newActiveConfiguredProject);
    }

    #region ITreeConstruction

    async ValueTask<IRule?> IProjectTreeOperations.GetDependencyBrowseObjectRuleAsync(IDependencyWithBrowseObject dependency, ConfiguredProject? configuredProject, IProjectCatalogSnapshot? catalogs)
    {
        IImmutableDictionary<string, IPropertyPagesCatalog> namedCatalogs = await GetNamedCatalogsAsync();

        if (!namedCatalogs.TryGetValue(PropertyPageContexts.BrowseObject, out IPropertyPagesCatalog browseObjectsCatalog))
        {
            // Issue https://github.com/dotnet/project-system/issues/4860 suggests this code path
            // can exist, however a repro was not found to dig deeper into the underlying cause.
            // For now just return null as the upstream caller handles null correctly anyway.
            return null;
        }

        var context = ProjectPropertiesContext.GetContext(
            UnconfiguredProject,
            itemType: dependency.SchemaItemType,
            itemName: dependency.Id);

        Rule? schema = dependency.SchemaName is not null
            ? browseObjectsCatalog.GetSchema(dependency.SchemaName)
            : null;

        if (schema is null)
        {
            // Since we have no browse object, we still need to create *something* so
            // that standard property pages can pop up.
            Rule emptyRule = new()
            {
                DataSource = new DataSource { ItemType = context.ItemType, Persistence = "ProjectFile" }
            };

            return GetConfiguredProjectExports().PropertyPagesDataModelProvider.GetRule(
                emptyRule,
                context.File,
                context.ItemType,
                context.ItemName);
        }

        if (dependency.UseResolvedReferenceRule)
        {
            return GetConfiguredProjectExports().RuleFactory.CreateResolvedReferencePageRule(
                schema,
                context,
                dependency.Id,
                dependency.BrowseObjectProperties);
        }

        return browseObjectsCatalog.BindToContext(schema.Name, context);

        async ValueTask<IImmutableDictionary<string, IPropertyPagesCatalog>> GetNamedCatalogsAsync()
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

            Assumes.NotNull(_namedCatalogs);

            return _namedCatalogs;
        }

        ConfiguredProjectExports GetConfiguredProjectExports()
        {
            configuredProject ??= ActiveConfiguredProject;

            Assumes.NotNull(configuredProject);

            return GetActiveConfiguredProjectExports(configuredProject);
        }
    }

    IProjectTree2 IProjectTreeOperations.NewTree(string caption, string? filePath, IRule? browseObjectProperties, ProjectImageMoniker? icon, ProjectImageMoniker? expandedIcon, bool visible, ProjectTreeFlags? flags, int displayOrder)
    {
        return NewTree(caption, filePath, browseObjectProperties, icon, expandedIcon, visible, flags, displayOrder);
    }

    IProjectItemTree2 IProjectTreeOperations.NewTree(string caption, IProjectPropertiesContext item, IPropertySheet? propertySheet, IRule? browseObjectProperties, ProjectImageMoniker? icon, ProjectImageMoniker? expandedIcon, bool visible, ProjectTreeFlags? flags, bool isLinked, int displayOrder)
    {
        return NewTree(caption, item, propertySheet, browseObjectProperties, icon, expandedIcon, visible, flags, isLinked, displayOrder);
    }

    #endregion

    /// <summary>
    /// Describes services collected from the active configured project.
    /// </summary>
    [Export]
    private sealed class MyConfiguredProjectExports : ConfiguredProjectExports
    {
        [ImportingConstructor]
        public MyConfiguredProjectExports(ConfiguredProject configuredProject)
            : base(configuredProject)
        {
        }
    }

    /// <summary>
    /// A private implementation of <see cref="IProjectTreeActionHandlerContext"/> for use with
    /// <see cref="IProjectTreeActionHandler"/> exports.
    /// </summary>
    private sealed class ProjectDependencyTreeRemovalActionHandlerContext : IProjectTreeActionHandlerContext
    {
        public IProjectTreeProvider TreeProvider { get; }

        public IProjectTreeActionHandler SuccessorHandlerDelegator => throw new NotImplementedException();

        public ProjectDependencyTreeRemovalActionHandlerContext(IProjectTreeProvider treeProvider)
        {
            TreeProvider = treeProvider;
        }
    }
}
