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
    private readonly IManagedProjectDiagnosticOutputService _logger;
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
        IManagedProjectDiagnosticOutputService logger,
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
        JoinableTaskCollection joinableTaskCollection,
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
            joinableTaskCollection,
            joinableTaskFactory,
            _threadingService.JoinableTaskContext,
            unloadCancellationToken: cancellationToken);

        ITargetBlock<IProjectVersionedValue<WorkspaceUpdate>> actionBlock
            = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<WorkspaceUpdate>>(
                target: workspace.OnWorkspaceUpdateAsync,
                project: _unconfiguredProject,
                severity: ProjectFaultSeverity.LimitedFunctionality,
                nameFormat: "Workspace update handler {0}");

        // Notify the workspace if the dataflow faults, to avoid hanging during initialization
        // while waiting for data that won't ever arrive due to the fault.
        _ = actionBlock.Completion.ContinueWith(
            task => workspace.Fault(task.Exception),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

        #region Ordering evaluation before first build

        IPropagatorBlock<IProjectVersionedValue<WorkspaceUpdate>, IProjectVersionedValue<WorkspaceUpdate>> orderingBlock
            = CreateWorkspaceUpdateOrderingBlock();

        workspace.ChainDisposal(orderingBlock.LinkTo(actionBlock, DataflowOption.PropagateCompletion));

        #endregion

        #region Evaluation data

        var evaluationTransformBlock
            = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<(ConfiguredProject ConfiguredProject, IProjectSnapshot ProjectSnapshot, IProjectSubscriptionUpdate EvaluationRuleUpdate, IProjectSubscriptionUpdate SourceItemsUpdate)>, IProjectVersionedValue<WorkspaceUpdate>>
                (update => update.Derive(WorkspaceUpdate.FromEvaluation));

        workspace.ChainDisposal(new DisposableBag
        {
            ProjectDataSources.SyncLinkTo(
                source.ActiveConfiguredProjectSource.SourceBlock.SyncLinkOptions(),
                source.ProjectSource.SourceBlock.SyncLinkOptions(),
                source.ProjectRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(updateHandlers.EvaluationRules)),
                source.SourceItemsRuleSource.SourceBlock.SyncLinkOptions(),
                target: evaluationTransformBlock,
                linkOptions: DataflowOption.PropagateCompletion,
                cancellationToken: cancellationToken),

            evaluationTransformBlock.LinkTo(orderingBlock, DataflowOption.PropagateCompletion),

            ProjectDataSources.JoinUpstreamDataSources(joinableTaskFactory, _projectService.Services.FaultHandler, source.ActiveConfiguredProjectSource, source.ProjectSource, source.ProjectRuleSource, source.SourceItemsRuleSource)
        });

        #endregion

        #region Build data subscriptions

        var buildTransformBlock
            = DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<(ConfiguredProject ConfiguredProject, IProjectSubscriptionUpdate BuildUpdate)>, IProjectVersionedValue<WorkspaceUpdate>>
                (update => update.Derive(WorkspaceUpdate.FromBuild));

        workspace.ChainDisposal(new DisposableBag
        {
            ProjectDataSources.SyncLinkTo(
                source.ActiveConfiguredProjectSource.SourceBlock.SyncLinkOptions(),
                source.ProjectBuildRuleSource.SourceBlock.SyncLinkOptions(DataflowOption.WithRuleNames(ProjectBuildRuleName)),
                target: buildTransformBlock,
                linkOptions: DataflowOption.PropagateCompletion,
                cancellationToken: cancellationToken),

            buildTransformBlock.LinkTo(orderingBlock, DataflowOption.PropagateCompletion),

            ProjectDataSources.JoinUpstreamDataSources(joinableTaskFactory, _projectService.Services.FaultHandler, source.ActiveConfiguredProjectSource, source.ProjectBuildRuleSource)
        });

        #endregion

        return workspace;
    }

    /// <summary>
    /// Creates a dataflow block that reorders initial values to ensure that the first output item
    /// is evaluation data, even if build data arrives first.
    /// </summary>
    /// <remarks>
    /// We need that behaviour for the Workspace, which creates the Roslyn object in response
    /// to evaluation data, and needs that Roslyn object when build data is processed.
    /// This block works by buffering any build data that arrives ahead of the first evaluation
    /// data.
    /// </remarks>
    /// <returns></returns>
    internal static IPropagatorBlock<IProjectVersionedValue<WorkspaceUpdate>, IProjectVersionedValue<WorkspaceUpdate>> CreateWorkspaceUpdateOrderingBlock()
    {
        List<IProjectVersionedValue<WorkspaceUpdate>>? bufferedBuilds = new() { null! };

        return DataflowBlockSlim.CreateTransformManyBlock<IProjectVersionedValue<WorkspaceUpdate>, IProjectVersionedValue<WorkspaceUpdate>>(
            input =>
            {
                if (bufferedBuilds is not null)
                {
                    if (input.Value.EvaluationUpdate is not null)
                    {
                        if (bufferedBuilds is { Count: 1 })
                        {
                            // First evaluation data, and no build data was buffered. Handle normally.
                            bufferedBuilds = null!;
                        }
                        else
                        {
                            // First evaluation data, and we have buffered build data.
                            // Prepend the evaluation in the first slot (we reserved an empty position here).
                            bufferedBuilds[0] = input;
                            // Null out the reference for future callers, and return this collection of output
                            // items for this input.
                            IEnumerable<IProjectVersionedValue<WorkspaceUpdate>> result = bufferedBuilds;
                            bufferedBuilds = null;
                            return result;
                        }
                    }
                    else if (input.Value.BuildUpdate is not null)
                    {
                        // We are buffering build data until some later point at which evaluation
                        // data arrives. Add it to the queue.
                        bufferedBuilds.Add(input);

                        // Return an empty enumerable. We don't yet want to produce any outputs.
                        return Enumerable.Empty<IProjectVersionedValue<WorkspaceUpdate>>();
                    }
                    else
                    {
                        throw Assumes.NotReachable();
                    }
                }

                // If we got here, we return the input item unchanged (just wrapped in an array).
                return new[] { input };
            },
            new ExecutionDataflowBlockOptions { NameFormat = "Workspace update ordering {0}" });
    }
}
