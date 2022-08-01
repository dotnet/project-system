// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

/// <summary>
/// Constructs <see cref="Workspace"/> instances and establishes the Dataflow subscriptions that
/// keep such instances updated over time as project data changes.
/// </summary>
[Export(typeof(IWorkspaceFactory))]
internal class WorkspaceFactory : IWorkspaceFactory
{
    private const string ProjectBuildRuleName = CompilerCommandLineArgs.SchemaName;

    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly IProjectService _projectService;
    private readonly IProjectThreadingService _threadingService;
    private readonly IProjectDiagnosticOutputService _logger;
    private readonly IDataProgressTrackerService _dataProgressTrackerService;
    private readonly IActiveEditorContextTracker _activeWorkspaceProjectContextTracker;
    private readonly IProjectFaultHandlerService _faultHandlerService;
    private readonly Lazy<IWorkspaceProjectContextFactory> _workspaceProjectContextFactory; // From Roslyn, so lazy // TODO why? requires main thread? do we need lazy here?
    private readonly ExportFactory<IWorkspaceUpdateHandler>[] _handlerFactories;

    [ImportMany]
    private readonly OrderPrecedenceImportCollection<ICommandLineParserService> _commandLineParsers;

    [ImportingConstructor]
    public WorkspaceFactory(
        UnconfiguredProject unconfiguredProject,
        IProjectService projectService,
        IProjectThreadingService threadingService,
        IUnconfiguredProjectTasksService tasksService,
        IProjectDiagnosticOutputService logger,
        IDataProgressTrackerService dataProgressTrackerService,
        IActiveEditorContextTracker activeWorkspaceProjectContextTracker,
        IProjectFaultHandlerService faultHandlerService,
        Lazy<IWorkspaceProjectContextFactory> workspaceProjectContextFactory,
        [ImportMany] ExportFactory<IWorkspaceUpdateHandler>[] handlerFactories)
    {
        _unconfiguredProject = unconfiguredProject;
        _projectService = projectService;
        _threadingService = threadingService;
        _logger = logger;
        _dataProgressTrackerService = dataProgressTrackerService;
        _activeWorkspaceProjectContextTracker = activeWorkspaceProjectContextTracker;
        _faultHandlerService = faultHandlerService;
        _workspaceProjectContextFactory = workspaceProjectContextFactory;
        _handlerFactories = handlerFactories;

        _commandLineParsers = new OrderPrecedenceImportCollection<ICommandLineParserService>(projectCapabilityCheckProvider: unconfiguredProject);
    }

    public Workspace Create(
        IActiveConfigurationSubscriptionSource source,
        ProjectConfigurationSlice slice,
        JoinableTaskFactory joinableTaskFactory,
        Guid projectGuid,
        CancellationToken cancellationToken)
    {
        UpdateHandlers updateHandlers = new(_handlerFactories);

        Workspace workspace = new(
            slice,
            _unconfiguredProject,
            projectGuid,
            updateHandlers,
            _logger,
            _activeWorkspaceProjectContextTracker,
            _commandLineParsers,
            _dataProgressTrackerService,
            _workspaceProjectContextFactory,
            _faultHandlerService,
            joinableTaskFactory,
            _threadingService.JoinableTaskContext,
            unloadCancellationToken: cancellationToken);

        ITargetBlock<IProjectVersionedValue<WorkspaceUpdate>> actionBlock
            = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<WorkspaceUpdate>>(
                target: workspace.OnWorkspaceUpdateAsync,
                project: _unconfiguredProject,
                severity: ProjectFaultSeverity.LimitedFunctionality,
                nameFormat: "Workspace update handler {0}");

        #region Evaluation data

        var evaluationTransformBlock
            = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<(ConfiguredProject ConfiguredProject, IProjectSubscriptionUpdate EvaluationRuleUpdate, IProjectSubscriptionUpdate SourceItemsUpdate)>, IProjectVersionedValue<WorkspaceUpdate>>
                (update => update.Derive(WorkspaceUpdate.FromEvaluation));

        workspace.ChainDisposal(new DisposableBag
        {
            ProjectDataSources.SyncLinkTo(
                source.ActiveConfiguredProjectSource.SourceBlock.SyncLinkOptions(),
                source.ProjectRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(updateHandlers.EvaluationRules)),
                source.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                target: evaluationTransformBlock,
                linkOptions: DataflowOption.PropagateCompletion,
                cancellationToken: cancellationToken),

            evaluationTransformBlock.LinkTo(actionBlock, DataflowOption.PropagateCompletion),

            ProjectDataSources.JoinUpstreamDataSources(joinableTaskFactory, _projectService.Services.FaultHandler, source.ProjectRuleSource, source.SourceItemsRuleSource)
        });

        #endregion

        #region Build data subscriptions

        // A block that provides the command line arguments for a configured project.
        // We will link this to the slice's active configured project.
        // NOTE this will go away once CPS exposes a way to access snapshot data while preserving order (being worked on for 17.4)
        var commandLineArgumentsBlock = new UnwrapChainedProjectValueDataSource<ConfiguredProject, CommandLineArgumentsSnapshot>(
            _unconfiguredProject,
            configuredProject => configuredProject.Services.ExportProvider.GetExportedValue<ICommandLineArgumentsDataSource>());

        var buildTransformBlock = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<(ConfiguredProject ConfiguredProject, IProjectSubscriptionUpdate BuildUpdate, CommandLineArgumentsSnapshot CommandLineArgumentsSnapshot)>, IProjectVersionedValue<WorkspaceUpdate>>
            (update => update.Derive(WorkspaceUpdate.FromBuild));

        workspace.ChainDisposal(new DisposableBag
        {
            commandLineArgumentsBlock,

            source.ActiveConfiguredProjectSource.SourceBlock.LinkTo(commandLineArgumentsBlock, DataflowOption.PropagateCompletion),

            ProjectDataSources.SyncLinkTo(
                source.ActiveConfiguredProjectSource.SourceBlock.SyncLinkOptions(),
                source.ProjectBuildRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(ProjectBuildRuleName)),
                commandLineArgumentsBlock.SourceBlock.SyncLinkOptions(),
                target: buildTransformBlock,
                linkOptions: DataflowOption.PropagateCompletion),

            buildTransformBlock.LinkTo(actionBlock, DataflowOption.PropagateCompletion),

            ProjectDataSources.JoinUpstreamDataSources(joinableTaskFactory, _projectService.Services.FaultHandler, source.ActiveConfiguredProjectSource, source.ProjectBuildRuleSource, commandLineArgumentsBlock)
        });

        #endregion

        return workspace;
    }
}
