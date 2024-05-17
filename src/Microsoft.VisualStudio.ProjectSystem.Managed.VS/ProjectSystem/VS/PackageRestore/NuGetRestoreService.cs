// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.Performance;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using Microsoft.VisualStudio.Threading;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore;

[Export(typeof(INuGetRestoreService))]
[Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
[AppliesTo(ProjectCapability.PackageReferences)]
internal class NuGetRestoreService : OnceInitializedOnceDisposed, INuGetRestoreService, IProjectDynamicLoadComponent, IVsProjectRestoreInfoSource
{
    private readonly UnconfiguredProject _project;
    private readonly IVsSolutionRestoreService5 _solutionRestoreService;
    private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;
    private readonly IProjectFaultHandlerService _faultHandlerService;

    /// <summary>
    /// Save the configured project versions that might get nominations.
    /// </summary>
    private readonly Dictionary<ProjectConfiguration, IComparable> _savedNominatedConfiguredVersion = new();

    /// <summary>
    /// Re-usable task that completes when there is a new nomination
    /// </summary>
    private TaskCompletionSource<bool>? _whenNominatedTask;

    private bool _enabled;
    private bool _restoring;
    private bool _updatesCompleted;

    [ImportingConstructor]
    public NuGetRestoreService(
        UnconfiguredProject project,
        IVsSolutionRestoreService5 solutionRestoreService,
        [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService projectAsynchronousTasksService,
        IProjectFaultHandlerService faultHandlerService)
    {
        _project = project;
        _solutionRestoreService = solutionRestoreService;
        _projectAsynchronousTasksService = projectAsynchronousTasksService;
        _faultHandlerService = faultHandlerService;
    }

    public async Task<bool> NominateAsync(ProjectRestoreInfo restoreData, IReadOnlyCollection<PackageRestoreConfiguredInput> inputVersions, CancellationToken cancellationToken)
    {
        try
        {
            _restoring = true;
            
            Task<bool> restoreOperation = _solutionRestoreService.NominateProjectAsync(_project.FullPath, new VsProjectRestoreInfo(restoreData), cancellationToken);
            
            SaveNominatedConfiguredVersions(inputVersions);

            return await restoreOperation;
        }
        finally
        {
            CodeMarkers.Instance.CodeMarker(CodeMarkerTimerId.PerfPackageRestoreEnd);
            _restoring = false;
        }
    }

    public Task UpdateWithoutNominationAsync(IReadOnlyCollection<PackageRestoreConfiguredInput> inputVersions)
    {
        SaveNominatedConfiguredVersions(inputVersions);

        return Task.CompletedTask;
    }

    public void NotifyComplete()
    {
        lock (SyncObject)
        {
            _updatesCompleted = true;
            _whenNominatedTask?.TrySetCanceled();
        }
    }

    public void NotifyFaulted(Exception e)
    {
        lock (SyncObject)
        {
            _updatesCompleted = true;
            _whenNominatedTask?.SetException(e);
        }
    }

    public Task WhenNominated(CancellationToken cancellationToken)
    {
        lock (SyncObject)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!CheckIfHasPendingNomination())
            {
                return Task.CompletedTask;
            }

            _whenNominatedTask ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        return _whenNominatedTask.Task.WithCancellation(cancellationToken);
    }

    public string Name => _project.FullPath;

    public bool HasPendingNomination => CheckIfHasPendingNomination();

    public Task LoadAsync()
    {
        _enabled = true;

        EnsureInitialized();

        return Task.CompletedTask;
    }

    public Task UnloadAsync()
    {
        lock (SyncObject)
        {
            _enabled = false;

            _whenNominatedTask?.TrySetCanceled();
        }

        return Task.CompletedTask;
    }

    protected override void Initialize()
    {
        RegisterProjectRestoreInfoSource();
    }

    protected override void Dispose(bool disposing)
    {
    }

    private void SaveNominatedConfiguredVersions(IReadOnlyCollection<PackageRestoreConfiguredInput> configuredInputs)
    {
        lock (SyncObject)
        {
            _savedNominatedConfiguredVersion.Clear();

            foreach (var configuredInput in configuredInputs)
            {
                _savedNominatedConfiguredVersion[configuredInput.ProjectConfiguration] = configuredInput.ConfiguredProjectVersion;
            }

            if (_whenNominatedTask is not null)
            {
                if (_whenNominatedTask.TrySetResult(true))
                {
                    _whenNominatedTask = null;
                }
            }
        }
    }

    private bool CheckIfHasPendingNomination()
    {
        lock (SyncObject)
        {
            Assumes.Present(_project.Services.ActiveConfiguredProjectProvider);
            Assumes.Present(_project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject);

            // Nuget should not wait for projects that failed DTB
            if (!_enabled || _updatesCompleted)
            {
                return false;
            }

            // Avoid possible deadlock.
            // Because RestoreCoreAsync() is called inside a dataflow block it will not be called with new data
            // until the old task finishes. So, if the project gets nominating restore, it will not get updated data.
            if (_restoring)
            {
                return false;
            }

            ConfiguredProject? activeConfiguredProject = _project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject;

            // After the first nomination, we should check the saved nominated version
            return IsSavedNominationOutOfDate(activeConfiguredProject);
        }
    }

    private bool IsSavedNominationOutOfDate(ConfiguredProject activeConfiguredProject)
    {
        if (!_savedNominatedConfiguredVersion.TryGetValue(activeConfiguredProject.ProjectConfiguration,
                out IComparable latestSavedVersionForActiveConfiguredProject) ||
            activeConfiguredProject.ProjectVersion.IsLaterThan(latestSavedVersionForActiveConfiguredProject))
        {
            return true;
        }
        
        if (_savedNominatedConfiguredVersion.Count == 1)
        {
            return false;
        }

        foreach (ConfiguredProject loadedProject in activeConfiguredProject.UnconfiguredProject.LoadedConfiguredProjects)
        {
            if (_savedNominatedConfiguredVersion.TryGetValue(loadedProject.ProjectConfiguration, out IComparable savedProjectVersion))
            {
                if (loadedProject.ProjectVersion.IsLaterThan(savedProjectVersion))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void RegisterProjectRestoreInfoSource()
    {
        // Register before this project receives any data flows containing possible nominations.
        // This is needed because we need to register before any nuget restore or before the solution load.
#pragma warning disable RS0030 // Do not used banned APIs
        var registerRestoreInfoSourceTask = Task.Run(() =>
        {
            return _solutionRestoreService.RegisterRestoreInfoSourceAsync(this, _projectAsynchronousTasksService.UnloadCancellationToken);
        });
#pragma warning restore RS0030 // Do not used banned APIs

        _faultHandlerService.Forget(registerRestoreInfoSourceTask, _project, ProjectFaultSeverity.Recoverable);
    }
}
