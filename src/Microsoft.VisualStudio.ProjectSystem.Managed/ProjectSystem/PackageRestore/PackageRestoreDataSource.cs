// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Notifications;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Responsible for pushing ("nominating") project data such as referenced packages and
    ///     target frameworks to NuGet so that it can perform a package restore and returns the
    ///     result.
    /// </summary>
    [Export(typeof(IPackageRestoreDataSource))]
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class PackageRestoreDataSource : ChainedProjectValueDataSourceBase<RestoreData>, IPackageRestoreDataSource, IProjectDynamicLoadComponent
    {
        // This class represents the last data source in the package restore chain, which is made up of the following:
        //  _________________________________________      _________________________________________
        // |                                         |    |                                         |
        // | PackageRestoreConfiguredInputDataSource |    | PackageRestoreConfiguredInputDataSource |  Produces data for each "implicitly active" config
        // |            (Debug|AnyCPU|net45)         |    |       (Debug|AnyCPU|netcoreapp30)       |
        // |_________________________________________|    |_________________________________________|
        //                                 ||                         ||
        //                                 PackageRestoreConfiguredInput
        //                                 ||                         ||
        //                         ________\/_________________________\/______
        //                        |                                           |
        //                        | PackageRestoreUnconfiguredInputDataSource |                        Merges config data into single input
        //                        |___________________________________________|
        //                                             ||
        //                                PackageRestoreUnconfiguredInput
        //                                             || 
        //                         ____________________\/_____________________      ___________________________
        //                        |                                           |    |                           |
        //                        |         PackageRestoreDataSource          |===>|    NuGetRestoreService    |  Pushes restore data to NuGet
        //                        |___________________________________________|    |___________________________|
        //                                             ||
        //                                        RestoreData
        //                                        //        \\ 
        //                                       //          \\
        //  ____________________________________\/___      ___\/____________________________________
        // |                                         |    |                                         |
        // |        PackageRestoreProgressTracker    |    |       PackageRestoreProgressTracker     |  Publishes restore progress to operation progress
        // |            (Debug|AnyCPU|net45)         |    |       (Debug|AnyCPU|netcoreapp30)       |
        // |_________________________________________|    |_________________________________________|
        //

        private readonly Stopwatch _stopwatch = new();
        private readonly NuGetRestoreCycleDetector _cycleDetector = new();
        private readonly IProjectSystemOptions _projectSystemOptions;
        private readonly ITelemetryService _telemetryService;
        private readonly INonModalNotificationService _userNotificationService;
        private readonly UnconfiguredProject _project;
        private readonly IPackageRestoreUnconfiguredInputDataSource _dataSource;
        private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;
        private readonly IFileSystem _fileSystem;
        private readonly IManagedProjectDiagnosticOutputService _logger;
        private readonly INuGetRestoreService _nuGetRestoreService;
        private byte[]? _latestHash;
        private bool _enabled;
        private bool _wasSourceBlockContinuationSet;
        private int _nuGetRestoreSuccesses;
        private int _nuGetRestoreCyclesDetected;

        [ImportingConstructor]
        public PackageRestoreDataSource(
            IProjectSystemOptions projectSystemOptions,
            ITelemetryService telemetryService,
            INonModalNotificationService userNotificationService,
            UnconfiguredProject project,
            IPackageRestoreUnconfiguredInputDataSource dataSource,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService projectAsynchronousTasksService,
            IFileSystem fileSystem,
            IManagedProjectDiagnosticOutputService logger,
            PackageRestoreSharedJoinableTaskCollection sharedJoinableTaskCollection,
            INuGetRestoreService nuGetRestoreService)
            : base(project, sharedJoinableTaskCollection, synchronousDisposal: true, registerDataSource: false)
        {
            _projectSystemOptions = projectSystemOptions;
            _telemetryService = telemetryService;
            _userNotificationService = userNotificationService;
            _project = project;
            _dataSource = dataSource;
            _projectAsynchronousTasksService = projectAsynchronousTasksService;
            _fileSystem = fileSystem;
            _logger = logger;
            _nuGetRestoreService = nuGetRestoreService;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<RestoreData>> targetBlock)
        {
            JoinUpstreamDataSources(_dataSource);

            // Take the unconfigured "restore inputs", send them to NuGet, and then return the result of that restore
            // We make use of TransformMany so that we can opt out of returning
            DisposableValue<ISourceBlock<IProjectVersionedValue<RestoreData>>> transformBlock = _dataSource.SourceBlock.TransformManyWithNoDelta(RestoreAsync);

            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return transformBlock;
        }

        internal async Task<IEnumerable<IProjectVersionedValue<RestoreData>>> RestoreAsync(IProjectVersionedValue<PackageRestoreUnconfiguredInput> e)
        {
            // No configurations - likely during project close.
            // Check if out of date to prevent extra restore under some conditions.
            if (!_enabled || e.Value.RestoreInfo is null || IsProjectConfigurationVersionOutOfDate(e.Value.ConfiguredInputs))
            {
                return Enumerable.Empty<IProjectVersionedValue<RestoreData>>();
            }

            bool succeeded = await RestoreCoreAsync(e.Value);

            RestoreData restoreData = CreateRestoreData(e.Value.RestoreInfo, succeeded);

            return new[]
            {
                new ProjectVersionedValue<RestoreData>(restoreData, e.DataSourceVersions)
            };
        }

        private async Task<bool> RestoreCoreAsync(PackageRestoreUnconfiguredInput value)
        {
            ProjectRestoreInfo? restoreInfo = value.RestoreInfo;
            bool success = false;

            Assumes.NotNull(restoreInfo);

            // Restore service always does work regardless of whether the value we pass 
            // them to actually contains changes, only nominate if there are any.
            byte[] hash = RestoreHasher.CalculateHash(restoreInfo);

            if (_latestHash is not null && hash.AsSpan().SequenceEqual(_latestHash))
            {
                _stopwatch.Reset();
                _nuGetRestoreSuccesses++;
                _cycleDetector.Clear();
                await _nuGetRestoreService.UpdateWithoutNominationAsync(value.ConfiguredInputs);
                return true;
            }

            _latestHash = hash;

            if (await CycleDetectedAsync(hash, _projectAsynchronousTasksService.UnloadCancellationToken))
            {
                _nuGetRestoreCyclesDetected++;
                return false;
            }
                
            JoinableTask<bool> joinableTask = JoinableFactory.RunAsync(() =>
            {
                return NominateForRestoreAsync(restoreInfo, value.ConfiguredInputs, _projectAsynchronousTasksService.UnloadCancellationToken);
            });

            _projectAsynchronousTasksService.RegisterAsyncTask(joinableTask,
                                                                ProjectCriticalOperation.Build | ProjectCriticalOperation.Unload | ProjectCriticalOperation.Rename,
                                                                registerFaultHandler: true);

            success = await joinableTask;

            return success;
        }

        private async Task<bool> CycleDetectedAsync(byte[] hash, CancellationToken cancellationToken)
        {
            _stopwatch.Start();

            bool enabled = await _projectSystemOptions.GetDetectNuGetRestoreCyclesAsync(cancellationToken);

            if (!enabled)
            {
                return false;
            }

            bool cycleDetected = _cycleDetector.ComputeCycleDetection(hash);

            if (!cycleDetected)
            {
                return false;
            }

            _stopwatch.Stop();

            SendTelemetry();

            await _userNotificationService.ShowErrorAsync(
                Resources.Restore_NuGetCycleDetected,
                cancellationToken
            );

            return true;

            void SendTelemetry()
            {
                _telemetryService.PostProperties(TelemetryEventName.NuGetRestoreCycleDetected, new[]
                {
                    (TelemetryPropertyName.NuGetRestoreCycleDetected.RestoreDurationMillis, (object)_stopwatch.Elapsed.TotalMilliseconds),
                    (TelemetryPropertyName.NuGetRestoreCycleDetected.RestoreSuccesses, _nuGetRestoreSuccesses),
                    (TelemetryPropertyName.NuGetRestoreCycleDetected.RestoreCyclesDetected, _nuGetRestoreCyclesDetected)
                });
            }
        }

        private async Task<bool> NominateForRestoreAsync(ProjectRestoreInfo restoreInfo, IReadOnlyCollection<PackageRestoreConfiguredInput> versions, CancellationToken cancellationToken)
        {
            RestoreLogger.BeginNominateRestore(_logger, _project.FullPath, restoreInfo);

            try
            {
                return await _nuGetRestoreService.NominateAsync(restoreInfo, versions, cancellationToken);
            }
            finally
            {
                RestoreLogger.EndNominateRestore(_logger, _project.FullPath);
            }
        }

        private RestoreData CreateRestoreData(ProjectRestoreInfo restoreInfo, bool succeeded)
        {
            string projectAssetsFilePath = restoreInfo.ProjectAssetsFilePath;

            // Restore service gives us a guarantee that the assets file
            // will contain *at least* the changes that we pushed to it.

            if (projectAssetsFilePath.Length == 0)
                return new RestoreData(string.Empty, DateTime.MinValue, succeeded: false);

            DateTime lastWriteTime = _fileSystem.GetLastFileWriteTimeOrMinValueUtc(projectAssetsFilePath);

            return new RestoreData(
                projectAssetsFilePath,
                lastWriteTime,
                succeeded: succeeded && lastWriteTime != DateTime.MinValue);
        }

        protected virtual bool IsProjectConfigurationVersionOutOfDate(IReadOnlyCollection<PackageRestoreConfiguredInput> configuredInputs)
        {
            Assumes.Present(_project.Services.ActiveConfiguredProjectProvider);
            Assumes.Present(_project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject);

            if (configuredInputs is null)
            {
                return false;
            }

            ConfiguredProject activeConfiguredProject = _project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject;

            IComparable? activeProjectConfigurationVersionFromConfiguredInputs = null;
            foreach (PackageRestoreConfiguredInput configuredInput in configuredInputs)
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

            foreach (ConfiguredProject loadedProject in activeConfiguredProject.UnconfiguredProject.LoadedConfiguredProjects)
            {
                foreach (PackageRestoreConfiguredInput configuredInput in configuredInputs)
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

        public Task LoadAsync()
        {
            _enabled = true;

            EnsureInitialized();

            if (!_wasSourceBlockContinuationSet)
            {
                _wasSourceBlockContinuationSet = true;
                
                // Inform the NuGet restore service when there will be no further updates so it can cancel any pending work.
                _ = SourceBlock.Completion.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _nuGetRestoreService.UpdatesFaulted(t.Exception);
                    }
                    else
                    {
                        _nuGetRestoreService.UpdatesComplete();
                    }
                }, TaskScheduler.Default);
            }

            return Task.CompletedTask;
        }

        public Task UnloadAsync()
        {
            lock (SyncObject)
            {
                _enabled = false;
            }

            return Task.CompletedTask;
        }
    }
}
