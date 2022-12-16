// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.OperationProgress;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

/// <summary>
/// Models a Roslyn workspace, which is tied to a project configuration slice.
/// Subscribes to data for that slice, and populates the Roslyn workspace appropriately.
/// Is created and disposed along with the project's configuration slice.
/// </summary>
internal sealed class Workspace : OnceInitializedOnceDisposedUnderLockAsync, IWorkspace
{
    internal enum WorkspaceState
    {
        Uninitialized = 0,
        Initialized,
        Failed,
        Disposed
    }

    private const string ProjectBuildRuleName = CompilerCommandLineArgs.SchemaName;

    private readonly DisposableBag _disposableBag;

    private readonly ProjectConfigurationSlice _slice;
    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly Guid _projectGuid;
    private readonly UpdateHandlers _updateHandlers;
    private readonly IManagedProjectDiagnosticOutputService _logger;
    private readonly IActiveEditorContextTracker _activeEditorContextTracker;
    private readonly OrderPrecedenceImportCollection<ICommandLineParserService> _commandLineParserServices;
    private readonly IDataProgressTrackerService _dataProgressTrackerService;
    private readonly Lazy<IWorkspaceProjectContextFactory> _workspaceProjectContextFactory;
    private readonly IProjectFaultHandlerService _faultHandlerService;
    private readonly JoinableTaskCollection _joinableTaskCollection;
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly CancellationToken _unloadCancellationToken;
    private readonly string _baseDirectory;

    /// <summary>Completes when the workspace has integrated build data.</summary>
    private readonly TaskCompletionSource _contextCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The current state of this workspace.</summary>
    private WorkspaceState _state;

    /// <summary>Operation progress reporting for evaluation dataflow, as this blocks IntelliSense.</summary>
    private IDataProgressTrackerServiceRegistration? _evaluationProgressRegistration;

    /// <summary>Operation progress reporting for build dataflow, as this blocks IntelliSense.</summary>
    private IDataProgressTrackerServiceRegistration? _buildProgressRegistration;

    /// <summary>The Roslyn context object that backs this workspace.</summary>
    private IWorkspaceProjectContext? _context;

    /// <summary>Roslyn's identifier for this workspace context.</summary>
    private string? _contextId;

    /// <summary>Whether we have seen evaluation data yet.</summary>
    private bool _seenEvaluation;

    /// <summary>Gets whether this workspace represents the primary active configuration.</summary>
    public bool IsPrimary { get; internal set; }

    #region IWorkspace

    public IWorkspaceProjectContext Context => _context ?? throw new InvalidOperationException("Workspace has not been initialized.");

    public string ContextId => _contextId ?? throw new InvalidOperationException("Workspace has not been initialized.");

    public object HostSpecificErrorReporter => Context;

    #endregion

    internal Workspace(
        ProjectConfigurationSlice slice,
        UnconfiguredProject unconfiguredProject,
        Guid projectGuid,
        UpdateHandlers updateHandlers,
        IManagedProjectDiagnosticOutputService logger,
        IActiveEditorContextTracker activeEditorContextTracker,
        OrderPrecedenceImportCollection<ICommandLineParserService> commandLineParserServices,
        IDataProgressTrackerService dataProgressTrackerService,
        Lazy<IWorkspaceProjectContextFactory> workspaceProjectContextFactory,
        IProjectFaultHandlerService faultHandlerService,
        JoinableTaskCollection joinableTaskCollection,
        JoinableTaskFactory joinableTaskFactory,
        JoinableTaskContextNode joinableTaskContextNode,
        CancellationToken unloadCancellationToken)
        : base(joinableTaskContextNode)
    {
        _slice = slice;
        _unconfiguredProject = unconfiguredProject;
        _projectGuid = projectGuid;
        _updateHandlers = updateHandlers;
        _logger = logger;
        _activeEditorContextTracker = activeEditorContextTracker;
        _commandLineParserServices = commandLineParserServices;
        _dataProgressTrackerService = dataProgressTrackerService;
        _workspaceProjectContextFactory = workspaceProjectContextFactory;
        _faultHandlerService = faultHandlerService;
        _joinableTaskCollection = joinableTaskCollection;
        _joinableTaskFactory = joinableTaskFactory;
        _unloadCancellationToken = unloadCancellationToken;

        _baseDirectory = Path.GetDirectoryName(_unconfiguredProject.FullPath);

        // We take ownership of the lifetime of the provided update handlers, and dispose them
        // when this workspace is disposed.
        _disposableBag = new() { updateHandlers };
    }

    protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task DisposeCoreUnderLockAsync(bool initialized)
    {
        _state = WorkspaceState.Disposed;

        _contextCreated.TrySetCanceled();

        _disposableBag.Dispose();

        IsPrimary = false;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds an object that will be disposed along with this <see cref="Workspace"/> instance.
    /// </summary>
    /// <param name="disposable">The object to dispose when this object is disposed.</param>
    public void ChainDisposal(IDisposable disposable)
    {
        Verify.NotDisposed(this);

        _disposableBag.Add(disposable);
    }

    /// <summary>
    /// Integrates project updates into the workspace.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method must always receive an evaluation update first. After that point,
    /// both evaluation and build updates may arrive in any order, so long as values
    /// of each type are ordered correctly.
    /// </para>
    /// <para>
    /// Calls must not overlap. This method is not thread-safe. This method is designed
    /// to be called from a dataflow ActionBlock, which will serialize calls, so we
    /// needn't perform any locking or protection here.
    /// </para>
    /// </remarks>
    /// <param name="update">The project update to integrate.</param>
    /// <returns>A task that completes when the update has been integrated.</returns>
    internal async Task OnWorkspaceUpdateAsync(IProjectVersionedValue<WorkspaceUpdate> update)
    {
        Verify.NotDisposed(this);

        await InitializeAsync(_unloadCancellationToken);

        Assumes.True(_state is WorkspaceState.Uninitialized or WorkspaceState.Initialized);

        await _joinableTaskFactory.RunAsync(
            async () =>
            {
                // Calls never overlap. No synchronisation is needed here.
                // We can receive either evaluation OR build data first.

                if (TryTransition(WorkspaceState.Uninitialized, WorkspaceState.Initialized))
                {
                    // Note that we create operation progress registrations using the first primary (active) configuration
                    // within the slice. Over time this may change, but we keep the same registration to the first seen.

                    ConfiguredProject configuredProject = update.Value switch
                    {
                        { EvaluationUpdate: EvaluationUpdate update } => update.ConfiguredProject,
                        { BuildUpdate: BuildUpdate update } => update.ConfiguredProject,
                        _ => throw Assumes.NotReachable()
                    };

                    _evaluationProgressRegistration = _dataProgressTrackerService.RegisterForIntelliSense(this, configuredProject, "LanguageServiceHost.Workspace.Evaluation");
                    _buildProgressRegistration = _dataProgressTrackerService.RegisterForIntelliSense(this, configuredProject, "LanguageServiceHost.Workspace.ProjectBuild");

                    _disposableBag.Add(_evaluationProgressRegistration);
                    _disposableBag.Add(_buildProgressRegistration);
                }

                try
                {
                    await (update.Value switch
                    {
                        { EvaluationUpdate: not null } => OnEvaluationUpdateAsync(update.Derive(u => u.EvaluationUpdate!)),
                        { BuildUpdate: not null } => OnBuildUpdateAsync(update.Derive(u => u.BuildUpdate!)),
                        _ => throw Assumes.NotReachable()
                    });
                }
                catch
                {
                    // Tear down on any exception
                    await DisposeAsync();

                    // Exceptions here are product errors, so let the exception escape in order
                    // to produce an upstream NFE.
                    throw;
                }
            });
    }

    private async Task OnEvaluationUpdateAsync(IProjectVersionedValue<EvaluationUpdate> evaluationUpdate)
    {
        Assumes.True(_state is WorkspaceState.Initialized);
        Assumes.NotNull(_evaluationProgressRegistration);

        if (!_seenEvaluation)
        {
            _seenEvaluation = true;

            await ProcessInitialEvaluationDataAsync(_unloadCancellationToken);

            // If initialisation failed gracefully, we will have been disposed and should just return here.
            if (IsDisposed)
            {
                return;
            }
        }

        await OnProjectChangedAsync(
            _evaluationProgressRegistration,
            evaluationUpdate,
            hasChange: e => HasChange(e.Value.EvaluationRuleUpdate) || e.Value.SourceItemsUpdate.ProjectChanges.HasChange(),
            applyFunc: ApplyProjectEvaluation,
            _unloadCancellationToken);

        return;

        async Task ProcessInitialEvaluationDataAsync(CancellationToken cancellationToken)
        {
            _logger.WriteLine("Initializing workspace from evaluation data");

            IProjectRuleSnapshot snapshot = evaluationUpdate.Value.EvaluationRuleUpdate.CurrentState[ConfigurationGeneral.SchemaName];

            snapshot.Properties.TryGetValue(ConfigurationGeneral.LanguageServiceNameProperty, out string? languageName);
            snapshot.Properties.TryGetValue(ConfigurationGeneral.MSBuildProjectFullPathProperty, out string? projectFilePath);

            if (string.IsNullOrEmpty(languageName) || string.IsNullOrEmpty(projectFilePath))
            {
                // It's reasonable for a project to exist that doesn't contain enough data to initialize the language
                // service. For example, the ".ilproj" project is not uncommon and hits this code path.
                // In such cases, we don't want to throw here as that would fault the project. Instead, we
                // dispose the workspace tears down the dataflow subscription and clean everything up gracefully.
                await DisposeAsync();

                return;
            }

            try
            {
                ProjectInstance projectInstance = evaluationUpdate.Value.ProjectSnapshot.ProjectInstance;

                EvaluationDataAdapter evaluationData = new(projectInstance);

                _contextId = GetWorkspaceProjectContextId(projectFilePath, _projectGuid, _slice);

                _disposableBag.Add(_activeEditorContextTracker.RegisterContext(_contextId));

                object? hostObject = _unconfiguredProject.Services.HostObject;

                // Call into Roslyn to initialize language service for this project
                _context = await _workspaceProjectContextFactory.Value.CreateProjectContextAsync(
                    _projectGuid,
                    _contextId,
                    languageName,
                    evaluationData,
                    hostObject,
                    cancellationToken);

                evaluationData.Release();

                _contextCreated.TrySetResult();

                _disposableBag.Add(_context);
            }
            catch (Exception ex)
            {
                _state = WorkspaceState.Failed;

                await _faultHandlerService.ReportFaultAsync(ex, _unconfiguredProject, ProjectFaultSeverity.LimitedFunctionality);

                _context?.Dispose();

                // We will never initialize now. Ensure anyone waiting on initialization sees the error.
                _contextCreated.TrySetException(ex);

                throw;
            }
        }

        void ApplyProjectEvaluation(
            IProjectVersionedValue<EvaluationUpdate> update,
            ContextState contextState,
            CancellationToken cancellationToken)
        {
            IComparable version = GetConfiguredProjectVersion(update);

            ProcessProjectEvaluationHandlers();

            ProcessSourceItemsHandlers();

            void ProcessProjectEvaluationHandlers()
            {
                // This is the ConfiguredProject currently bound to the slice owned by this workspace.
                // It may change over time, such as in response to changing the active configuration,
                // for example from Debug to Release.
                ConfiguredProject configuredProject = update.Value.ConfiguredProject;

                foreach (IProjectEvaluationHandler evaluationHandler in _updateHandlers.EvaluationHandlers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IProjectChangeDescription projectChange = update.Value.EvaluationRuleUpdate.ProjectChanges[evaluationHandler.ProjectEvaluationRule];

                    if (projectChange.Difference.AnyChanges)
                    {
                        evaluationHandler.Handle(Context, configuredProject.ProjectConfiguration, version, projectChange, contextState, _logger);
                    }
                }
            }

            void ProcessSourceItemsHandlers()
            {
                IImmutableDictionary<string, IProjectChangeDescription> changes = update.Value.SourceItemsUpdate.ProjectChanges;

                if (changes.HasChange())
                {
                    foreach (ISourceItemsHandler sourceItemsHandler in _updateHandlers.SourceItemHandlers)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        sourceItemsHandler.Handle(Context, version, changes, contextState, _logger);
                    }
                }
            }
        }

        bool HasChange(IProjectSubscriptionUpdate evaluationUpdate)
        {
            foreach (string ruleName in _updateHandlers.EvaluationRules)
            {
                if (evaluationUpdate.ProjectChanges[ruleName].Difference.AnyChanges)
                {
                    return true;
                }
            }

            return false;
        }

        static string GetWorkspaceProjectContextId(string projectFilePath, Guid projectGuid, ProjectConfigurationSlice slice)
        {
            // WorkspaceContextId must be unique across the entire solution for the life of the solution, therefore as we fire
            // up a workspace context per implicitly active config, we factor in both the full path of the project, the GUID of
            // project and the name of the config. This will be unique across regardless of whether projects are added or renamed
            // to match this project's original name. We include file path to make debugging easier on the Roslyn side.
            //
            // NOTE: Roslyn also uses this name as the default "AssemblyName" until we explicitly set it, so we need to make
            // sure it doesn't contain any invalid path characters.
            //
            // For example:
            //      C:\Project\Project.csproj ({72B509BD-C502-4707-ADFD-E2D43867CF45})
            //      C:\Project\MultiTarget.csproj (net45 {72B509BD-C502-4707-ADFD-E2D43867CF45})
            //      C:\Project\MultiTarget.csproj (net6.0 {72B509BD-C502-4707-ADFD-E2D43867CF45})

            if (slice.Dimensions.Count == 0)
                return $"{projectFilePath} ({projectGuid.ToString("B").ToUpperInvariant()})";
            else
                return $"{projectFilePath} ({string.Join(";", slice.Dimensions.Values)} {projectGuid.ToString("B").ToUpperInvariant()})";
        }
    }

    private async Task OnBuildUpdateAsync(IProjectVersionedValue<BuildUpdate> update)
    {
        Assumes.True(_state is WorkspaceState.Initialized);
        Assumes.NotNull(_buildProgressRegistration);

        // The Roslyn workspace context is created when the first evaluation data arrives.
        // Upstream caller must ensure that build updates are delayed until the first
        // evaluation update is processed.
        Assumes.True(_seenEvaluation);

        await OnProjectChangedAsync(
            _buildProgressRegistration,
            update,
            hasChange: e => e.Value.BuildRuleUpdate.ProjectChanges[ProjectBuildRuleName].Difference.AnyChanges,
            applyFunc: ApplyProjectBuild,
            _unloadCancellationToken);

        return;

        void ApplyProjectBuild(
            IProjectVersionedValue<BuildUpdate> update,
            ContextState state,
            CancellationToken cancellationToken)
        {
            IProjectChangeDescription projectChange = update.Value.BuildRuleUpdate.ProjectChanges[ProjectBuildRuleName];

            ProcessCommandLine();

            ProcessProjectBuildFailure(projectChange.After);

            void ProcessCommandLine()
            {
                SetContextCommandLine();

                InvokeCommandLineUpdateHandlers();

                void SetContextCommandLine()
                {
                    var orderedSource = projectChange.After.Items as IDataWithOriginalSource<KeyValuePair<string, IImmutableDictionary<string, string>>>;

                    Assumes.NotNull(orderedSource);

                    // Pass command line to Roslyn
                    Context.SetOptions(orderedSource.SourceData.Select(pair => pair.Key).ToImmutableArray());
                }

                void InvokeCommandLineUpdateHandlers()
                {
                    ICommandLineParserService? parser = _commandLineParserServices.FirstOrDefault()?.Value;

                    Assumes.Present(parser);

                    IComparable version = GetConfiguredProjectVersion(update);

                    BuildOptions added   = parser.Parse(projectChange.Difference.AddedItems,   _baseDirectory);
                    BuildOptions removed = parser.Parse(projectChange.Difference.RemovedItems, _baseDirectory);

                    foreach (ICommandLineHandler commandLineHandler in _updateHandlers.CommandLineHandlers)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        commandLineHandler.Handle(Context, version, added, removed, state, _logger);
                    }
                }
            }

            void ProcessProjectBuildFailure(IProjectRuleSnapshot snapshot)
            {
                // If 'CompileDesignTime' didn't run due to a preceding failed target, or a failure in itself, IsEvaluationSucceeded returns false.
                //
                // We still forward those 'removes' of references, sources, etc onto Roslyn to avoid duplicate/incorrect results when the next
                // successful build occurs, because it will be diff between it and this failed build.

                bool succeeded = snapshot.IsEvaluationSucceeded();

                if (Context.LastDesignTimeBuildSucceeded != succeeded)
                {
                    _logger.WriteLine(succeeded ? "Last design-time build succeeded, turning semantic errors back on." : "Last design-time build failed, turning semantic errors off.");
                    Context.LastDesignTimeBuildSucceeded = succeeded;
                }
            }
        }
    }

    private bool TryTransition(WorkspaceState initialState, WorkspaceState newState)
    {
        if (_state == initialState)
        {
            _state = newState;
            return true;
        }

        return false;
    }

    private Task OnProjectChangedAsync<T>(
        IDataProgressTrackerServiceRegistration registration,
        IProjectVersionedValue<T> update,
        Func<IProjectVersionedValue<T>, bool> hasChange,
        Action<IProjectVersionedValue<T>, ContextState, CancellationToken> applyFunc,
        CancellationToken cancellationToken)
    {
        Assumes.True(_contextCreated.Task.Status == TaskStatus.RanToCompletion);

        return ExecuteUnderLockAsync(ApplyProjectChangesUnderLockAsync, cancellationToken);

        Task ApplyProjectChangesUnderLockAsync(CancellationToken cancellationToken)
        {
            if (!hasChange(update))
            {
                // No change since the last update. We must still update operation progress, but can skip creating a batch.
                UpdateProgressRegistration();

                return Task.CompletedTask;
            }

            return ApplyInBatchAsync();

            async Task ApplyInBatchAsync()
            {
                ContextState contextState = new(
                    isActiveEditorContext: _activeEditorContextTracker.IsActiveEditorContext(ContextId),
                    isActiveConfiguration: IsPrimary);

                Context.StartBatch();

                try
                {
                    applyFunc(update, contextState, cancellationToken);
                }
                finally
                {
                    await Context.EndBatchAsync();

                    UpdateProgressRegistration();
                }
            }

            void UpdateProgressRegistration()
            {
                // Notify operation progress that we've now processed these versions of our input, if they are
                // up-to-date with the latest version that produced, then we no longer considered "in progress".
                registration?.NotifyOutputDataCalculated(update.DataSourceVersions);
            }
        }
    }

    private static IComparable GetConfiguredProjectVersion(IProjectValueVersions update)
    {
        return update.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];
    }

    public async Task WriteAsync(Func<IWorkspace, Task> action, CancellationToken cancellationToken)
    {
        Requires.NotNull(action, nameof(action));
        Verify.NotDisposed(this);

        cancellationToken = CancellationTokenExtensions.CombineWith(_unloadCancellationToken, cancellationToken).Token;

        await WhenContextCreated(cancellationToken);

        await ExecuteUnderLockAsync(_ => action(this), cancellationToken);
    }

    public async Task<T> WriteAsync<T>(Func<IWorkspace, Task<T>> action, CancellationToken cancellationToken)
    {
        Requires.NotNull(action, nameof(action));
        Verify.NotDisposed(this);

        cancellationToken = CancellationTokenExtensions.CombineWith(_unloadCancellationToken, cancellationToken).Token;

        await WhenContextCreated(cancellationToken);

        return await ExecuteUnderLockAsync(_ => action(this), cancellationToken);
    }

    private async Task WhenContextCreated(CancellationToken cancellationToken)
    {
        Verify.NotDisposed(this);

        // Join the same collection that's used by our dataflow nodes, so that if we are called on
        // the main thread, we don't block anything that might prohibit dataflow from progressing
        // this workspace's initialisation (leading to deadlock).
        using (_joinableTaskCollection.Join())
        {
            // Ensure we have received enough data to create the context.
            await _contextCreated.Task.WithCancellation(cancellationToken);

            Verify.NotDisposed(this);
        }
    }

    /// <summary>
    /// Called when the dataflow that provides project data to <see cref="OnWorkspaceUpdateAsync(IProjectVersionedValue{WorkspaceUpdate})"/>
    /// faults for some reason. This allows the workspace to know that no further updates are coming.
    /// </summary>
    /// <param name="exception"></param>
    internal void Fault(Exception exception)
    {
        // Ensure we unblock any code waiting for initialization, and surface the error to the caller.
        _contextCreated.TrySetException(exception);
    }

    /// <summary>
    /// Maps from our property data snapshot to Roslyn's API for accessing the project's
    /// evaluation data.
    /// </summary>
    private sealed class EvaluationDataAdapter : EvaluationData
    {
        private ProjectInstance? _projectInstance;

        public EvaluationDataAdapter(ProjectInstance projectInstance)
        {
            _projectInstance = projectInstance;
        }

        public override string GetPropertyValue(string name)
        {
            Validate();

            string? value = _projectInstance.GetProperty(name)?.EvaluatedValue;

            // Return the empty string rather than null.
            return value ?? "";
        }

        public override ImmutableArray<string> GetItemValues(string name)
        {
            Validate();

            ICollection<ProjectItemInstance> items = _projectInstance.GetItems(itemType: name);

            if (items.Count == 0)
            {
                return ImmutableArray<string>.Empty;
            }

            return items.Select(item => item.EvaluatedInclude).ToImmutableArray();
        }

        [MemberNotNull(nameof(_projectInstance))]
        private void Validate()
        {
            if (_projectInstance is null)
            {
                throw new ObjectDisposedException(nameof(EvaluationDataAdapter));
            }
        }

        /// <summary>
        /// Ensures the inner reference to <see cref="ProjectInstance"/> is cleared,
        /// so Roslyn cannot accidentally retain a rooted reference to it. It's a large
        /// object and must be collected as soon as possible.
        /// </summary>
        internal void Release()
        {
            _projectInstance = null;
        }
    }
}
