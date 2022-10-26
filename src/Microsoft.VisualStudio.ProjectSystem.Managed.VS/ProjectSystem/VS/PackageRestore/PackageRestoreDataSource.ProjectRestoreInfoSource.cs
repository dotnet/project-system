// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using Microsoft.VisualStudio.Threading;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal partial class PackageRestoreDataSource : IVsProjectRestoreInfoSource
    {
        /// <summary>
        /// Re-usable task that completes when there is a new nomination
        /// </summary>
        private TaskCompletionSource<bool>? _whenNominatedTask;

        private bool _restoreStarted;

        private bool _wasSourceBlockContinuationSet;

        /// <summary>
        /// Save the configured project versions that might get nominations.
        /// </summary>
        private readonly Dictionary<ProjectConfiguration, IComparable> _savedNominatedConfiguredVersion = new();

        /// <summary>
        /// Project Unique Name used by Nuget Nomination.
        /// </summary>
        public string Name => _project.FullPath;

        // True means the project system plans to call NominateProjectAsync in the future.
        bool IVsProjectRestoreInfoSource.HasPendingNomination => CheckIfHasPendingNomination();

        // NuGet calls this method to wait project to nominate restoring.
        // If the project has no pending restore data, it will return a completed task.
        // Otherwise a task which will be completed once the project nominate the next restore
        // the task will be cancelled, if the project system decide it no longer need restore (for example: the restore state has no change)
        // the task will be failed, if the project system runs into a problem, so it cannot get correct data to nominate a restore (DT build failed)
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

                if (!_wasSourceBlockContinuationSet)
                {
                    _wasSourceBlockContinuationSet = true;

                    _ = SourceBlock.Completion.ContinueWith(t =>
                    {
                        lock (SyncObject)
                        {
                            if (t.IsFaulted)
                            {
                                _whenNominatedTask?.SetException(t.Exception);
                            }
                            else
                            {
                                _whenNominatedTask?.TrySetCanceled();
                            }
                        }
                    }, TaskScheduler.Default);
                }
            }

            return _whenNominatedTask.Task.WithCancellation(cancellationToken);
        }

        private void RegisterProjectRestoreInfoSource()
        {
            // Register before this project receives any data flows containing possible nominations.
            // This is needed because we need to register before any nuget restore or before the solution load.
#pragma warning disable RS0030 // Do not used banned APIs
            var registerRestoreInfoSourceTask = Task.Run(async () =>
            {
                await _solutionRestoreService4.RegisterRestoreInfoSourceAsync(this, _projectAsynchronousTasksService.UnloadCancellationToken);
            });
#pragma warning restore RS0030 // Do not used banned APIs

            _project.Services.FaultHandler.Forget(registerRestoreInfoSourceTask, _project, ProjectFaultSeverity.Recoverable);
        }

        private bool CheckIfHasPendingNomination()
        {
            lock (SyncObject)
            {
                Assumes.Present(_project.Services.ActiveConfiguredProjectProvider);
                Assumes.Present(_project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject);

                // Nuget should not wait for projects that failed DTB
                if (!_enabled || SourceBlock.Completion.IsFaulted || SourceBlock.Completion.IsCompleted)
                {
                    return false;
                }

                // Avoid possible deadlock.
                // Because RestoreCoreAsync() is called inside a dataflow block it will not be called with new data
                // until the old task finishes. So, if the project gets nominating restore, it will not get updated data.
                if (IsPackageRestoreOnGoing())
                {
                    return false;
                }

                ConfiguredProject? activeConfiguredProject = _project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject;

                // After the first nomination, we should check the saved nominated version
                return IsSavedNominationOutOfDate(activeConfiguredProject);
            }
        }

        private bool IsPackageRestoreOnGoing()
        {
            // If NominateForRestoreAsync() has not finished, return false
            return _restoreStarted;
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

        protected virtual bool IsSavedNominationOutOfDate(ConfiguredProject activeConfiguredProject)
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

            foreach (var loadedProject in activeConfiguredProject.UnconfiguredProject.LoadedConfiguredProjects)
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

        protected virtual bool IsProjectConfigurationVersionOutOfDate(IReadOnlyCollection<PackageRestoreConfiguredInput> configuredInputs)
        {
            Assumes.Present(_project.Services.ActiveConfiguredProjectProvider);
            Assumes.Present(_project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject);

            if (configuredInputs is null)
            {
                return false;
            }

            var activeConfiguredProject = _project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject;

            IComparable? activeProjectConfigurationVersionFromConfiguredInputs = null;
            foreach (var configuredInput in configuredInputs)
            {
                if (configuredInput.ProjectConfiguration.Equals(activeConfiguredProject.ProjectConfiguration))
                {
                    activeProjectConfigurationVersionFromConfiguredInputs = configuredInput.ConfiguredProjectVersion;
                }
            }

            if (activeProjectConfigurationVersionFromConfiguredInputs is null ||
                activeConfiguredProject.ProjectVersion.IsLaterThan(
                    activeProjectConfigurationVersionFromConfiguredInputs))
            {
                return true;
            }

            if (configuredInputs.Count == 1)
            {
                return false;
            }

            foreach (var loadedProject in activeConfiguredProject.UnconfiguredProject.LoadedConfiguredProjects)
            {
                foreach (var configuredInput in configuredInputs)
                {
                    if (loadedProject.ProjectConfiguration.Equals(configuredInput.ProjectConfiguration) &&
                        loadedProject.ProjectVersion.IsLaterThan(configuredInput.ConfiguredProjectVersion))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
