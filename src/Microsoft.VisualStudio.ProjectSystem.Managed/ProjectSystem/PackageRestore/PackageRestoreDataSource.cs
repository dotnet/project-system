// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
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

        private readonly IPackageRestoreCycleDetector _cycleDetector;
        private readonly UnconfiguredProject _project;
        private readonly IPackageRestoreUnconfiguredInputDataSource _dataSource;
        private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;
        private readonly IFileSystem _fileSystem;
        private readonly IManagedProjectDiagnosticOutputService _logger;
        private readonly INuGetRestoreService _nuGetRestoreService;
        private Hash? _lastHash;
        private bool _enabled;
        private bool _wasSourceBlockContinuationSet;

        [ImportingConstructor]
        public PackageRestoreDataSource(
            UnconfiguredProject project,
            PackageRestoreSharedJoinableTaskCollection sharedJoinableTaskCollection,
            IPackageRestoreUnconfiguredInputDataSource dataSource,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService projectAsynchronousTasksService,
            IFileSystem fileSystem,
            IManagedProjectDiagnosticOutputService logger,
            INuGetRestoreService nuGetRestoreService,
            IPackageRestoreCycleDetector cycleDetector)
            : base(project, sharedJoinableTaskCollection, synchronousDisposal: true, registerDataSource: false)
        {
            _project = project;
            _dataSource = dataSource;
            _projectAsynchronousTasksService = projectAsynchronousTasksService;
            _fileSystem = fileSystem;
            _logger = logger;
            _nuGetRestoreService = nuGetRestoreService;
            _cycleDetector = cycleDetector;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<RestoreData>> targetBlock)
        {
            JoinUpstreamDataSources(_dataSource);

            // Take the unconfigured "restore inputs", send them to NuGet, and then return the result of that restore.
            // We make use of TransformMany so that we can opt out of returning.
            DisposableValue<ISourceBlock<IProjectVersionedValue<RestoreData>>> transformBlock = _dataSource.SourceBlock.TransformManyWithNoDelta(RestoreAsync);

            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return transformBlock;
        }

        // internal for testing purposes only -- the only real caller is the transform block
        internal async Task<IEnumerable<IProjectVersionedValue<RestoreData>>> RestoreAsync(IProjectVersionedValue<PackageRestoreUnconfiguredInput> e)
        {
            // No configurations - likely during project close.
            // Check if out of date to prevent extra restore under some conditions.
            if (!_enabled || e.Value.RestoreInfo is null || IsRestoreDataVersionOutOfDate(e.DataSourceVersions) || IsProjectConfigurationVersionOutOfDate(e.Value.ConfiguredInputs))
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
            CancellationToken token = _projectAsynchronousTasksService.UnloadCancellationToken;

            ProjectRestoreInfo? restoreInfo = value.RestoreInfo;

            Assumes.NotNull(restoreInfo);

            // Restore service always does work regardless of whether the value we pass 
            // them to actually contains changes, only nominate if there are any.
            Hash hash = RestoreHasher.CalculateHash(restoreInfo);

            if (await _cycleDetector.IsCycleDetectedAsync(hash, token))
            {
                _lastHash = hash;
                return false;
            }

            if (_lastHash?.Equals(hash) == true)
            {
                await _nuGetRestoreService.UpdateWithoutNominationAsync(value.ConfiguredInputs);
                return true;
            }

            _lastHash = hash;

            JoinableTask<bool> joinableTask = JoinableFactory.RunAsync(() =>
            {
                return NominateForRestoreAsync(restoreInfo, value.ConfiguredInputs, token);
            });

            _projectAsynchronousTasksService.RegisterAsyncTask(
                joinableTask,
                operationFlags: ProjectCriticalOperation.Build | ProjectCriticalOperation.Unload | ProjectCriticalOperation.Rename,
                registerFaultHandler: true);

            return await joinableTask;
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

        protected virtual bool IsRestoreDataVersionOutOfDate(IImmutableDictionary<NamedIdentity, IComparable> dataVersions)
        {
            Assumes.Present(_project.Services.DataSourceRegistry);

            IProjectDataSourceRegistry dataSourceRegistry = _project.Services.DataSourceRegistry;
            foreach (KeyValuePair<NamedIdentity, IComparable> versionDescription in dataVersions)
            {
                if (dataSourceRegistry.TryGetDataSource(versionDescription.Key, out IProjectValueDataSource? dataSource) && versionDescription.Value.CompareTo(dataSource.DataSourceVersion) < 0)
                {
                    return true;
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
                        _nuGetRestoreService.NotifyFaulted(t.Exception);
                    }
                    else
                    {
                        _nuGetRestoreService.NotifyComplete();
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
