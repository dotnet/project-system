// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.Internal.Performance;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBarService;
using Microsoft.VisualStudio.Telemetry;
using NuGet.SolutionRestoreManager;
using System.Diagnostics;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using Microsoft.Internal.VisualStudio.Shell.Interop;

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
        //                         ____________________\/_____________________
        //                        |                                           |
        //                        |         PackageRestoreDataSource          |                        Pushes restore data to NuGet
        //                        |___________________________________________|
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
        private readonly IVsUIService<SVsFeatureFlags, IVsFeatureFlags> _featureFlagsService;
        private readonly ITelemetryService _telemetryService;
        private readonly IInfoBarService _infoBarService;
        private readonly UnconfiguredProject _project;
        private readonly IPackageRestoreUnconfiguredInputDataSource _dataSource;
        private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;
        private readonly IVsSolutionRestoreService3 _solutionRestoreService;
        private readonly IFileSystem _fileSystem;
        private readonly IManagedProjectDiagnosticOutputService _logger;
        private readonly IVsSolutionRestoreService4 _solutionRestoreService4;
        private byte[]? _latestHash;
        private bool _enabled;
        private int _nuGetRestoreSuccesses;
        private int _nuGetRestoreCyclesDetected;

        [ImportingConstructor]
        public PackageRestoreDataSource(
            IVsUIService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
            ITelemetryService telemetryService,
            IInfoBarService infoBarService,
            UnconfiguredProject project,
            IPackageRestoreUnconfiguredInputDataSource dataSource,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService projectAsynchronousTasksService,
            IVsSolutionRestoreService3 solutionRestoreService,
            IFileSystem fileSystem,
            IManagedProjectDiagnosticOutputService logger,
            IVsSolutionRestoreService4 solutionRestoreService4,
            PackageRestoreSharedJoinableTaskCollection sharedJoinableTaskCollection)
            : base(project, sharedJoinableTaskCollection, synchronousDisposal: true, registerDataSource: false)
        {
            _featureFlagsService = featureFlagsService;
            _telemetryService = telemetryService;
            _infoBarService = infoBarService;
            _project = project;
            _dataSource = dataSource;
            _projectAsynchronousTasksService = projectAsynchronousTasksService;
            _solutionRestoreService = solutionRestoreService;
            _fileSystem = fileSystem;
            _logger = logger;
            _solutionRestoreService4 = solutionRestoreService4;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<RestoreData>> targetBlock)
        {
            RegisterProjectRestoreInfoSource();

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

            try
            {
                // Restore service always does work regardless of whether the value we pass 
                // them to actually contains changes, only nominate if there are any.
                byte[] hash = RestoreHasher.CalculateHash(restoreInfo);

                if (_latestHash is not null && hash.AsSpan().SequenceEqual(_latestHash))
                {
                    _stopwatch.Reset();
                    _nuGetRestoreSuccesses++;
                    _cycleDetector.Clear();
                    SaveNominatedConfiguredVersions(value.ConfiguredInputs);
                    return true;
                }

                _latestHash = hash;

                if (await CycleDetectedAsync(hash, _projectAsynchronousTasksService.UnloadCancellationToken))
                {
                    _nuGetRestoreCyclesDetected++;
                    return false;
                }

                _restoreStarted = true;
                JoinableTask<bool> joinableTask = JoinableFactory.RunAsync(() =>
                {
                    return NominateForRestoreAsync(restoreInfo, _projectAsynchronousTasksService.UnloadCancellationToken);
                });

                SaveNominatedConfiguredVersions(value.ConfiguredInputs);

                _projectAsynchronousTasksService.RegisterAsyncTask(joinableTask,
                                                                   ProjectCriticalOperation.Build | ProjectCriticalOperation.Unload | ProjectCriticalOperation.Rename,
                                                                   registerFaultHandler: true);

                // Prevent overlap until Restore completes
                success = await joinableTask;

                lock (SyncObject)
                {
                    _restoreStarted = false;
                }
            }
            finally
            {
                _restoreStarted = false;
            }

            return success;
        }

        private async Task<bool> CycleDetectedAsync(byte[] hash, CancellationToken cancellationToken)
        {
            _stopwatch.Start();

            bool enabled = await IsNuGetRestoreCycleDetectionEnabledAsync(cancellationToken);

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

            await _infoBarService.ShowInfoBarAsync(
                VSResources.InfoBarMessageNuGetCycleDetected,
                KnownMonikers.StatusError,
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

        private async Task<bool> IsNuGetRestoreCycleDetectionEnabledAsync(CancellationToken cancellationToken)
        {
            await _project.Services.ThreadingPolicy.SwitchToUIThread(cancellationToken);

            IVsFeatureFlags featureFlagsService = _featureFlagsService.Value;

            return featureFlagsService.IsFeatureEnabled(FeatureFlagNames.EnableNuGetRestoreCycleDetection, defaultValue: false);
        }

        private async Task<bool> NominateForRestoreAsync(ProjectRestoreInfo restoreInfo, CancellationToken cancellationToken)
        {
            RestoreLogger.BeginNominateRestore(_logger, _project.FullPath, restoreInfo);

            try
            {
                return await _solutionRestoreService.NominateProjectAsync(_project.FullPath, restoreInfo, cancellationToken);
            }
            finally
            {
                CodeMarkers.Instance.CodeMarker(CodeMarkerTimerId.PerfPackageRestoreEnd);

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
    }
}
