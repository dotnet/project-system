// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Internal.Performance;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Microsoft.VisualStudio.Threading;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal partial class PackageRestoreService
    {
        internal class PackageRestoreServiceInstance : OnceInitializedOnceDisposedAsync, IMultiLifetimeInstance
        {
            private readonly UnconfiguredProject _project;
            private readonly IPackageRestoreUnconfiguredInputDataSource _dataSource;
            private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;
            private readonly IVsSolutionRestoreService3 _solutionRestoreService;
            private readonly IFileSystem _fileSystem;
            private readonly IProjectLogger _logger;
            private readonly IBroadcastBlock<IProjectVersionedValue<RestoreData>> _broadcastBlock;

            private IDisposable? _subscription;
            private ProjectRestoreInfo? _latestValue;

            public PackageRestoreServiceInstance(
                UnconfiguredProject project,
                IPackageRestoreUnconfiguredInputDataSource dataSource,
                IProjectThreadingService threadingService,
                IProjectAsynchronousTasksService projectAsynchronousTasksService,
                IVsSolutionRestoreService3 solutionRestoreService,
                IFileSystem fileSystem,
                IProjectLogger logger,
                IBroadcastBlock<IProjectVersionedValue<RestoreData>> broadcastBlock)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _dataSource = dataSource;
                _projectAsynchronousTasksService = projectAsynchronousTasksService;
                _solutionRestoreService = solutionRestoreService;
                _fileSystem = fileSystem;
                _logger = logger;
                _broadcastBlock = broadcastBlock;
            }

            public Task InitializeAsync()
            {
                return InitializeAsync(CancellationToken.None);
            }

            protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                _subscription = _dataSource.SourceBlock.LinkToAsyncAction(OnInputsChangedAsync);

                return Task.CompletedTask;
            }

            protected override Task DisposeCoreAsync(bool initialized)
            {
                _subscription?.Dispose();

                return Task.CompletedTask;
            }

            internal async Task OnInputsChangedAsync(IProjectVersionedValue<PackageRestoreUnconfiguredInput> e)
            {
                // No configurations - likely during project close
                if (e.Value.RestoreInfo is null)
                    return;

                try
                {
                    await RestoreAsync(e.Value.RestoreInfo);
                }
                finally
                {
                    OnRestoreCompleted(e);
                }
            }

            private void OnRestoreCompleted(IProjectVersionedValue<PackageRestoreUnconfiguredInput> e)
            {
                _broadcastBlock.Post(e.Derive(input => CreateRestoreData(input.RestoreInfo!)));
            }

            private async Task RestoreAsync(ProjectRestoreInfo restoreInfo)
            {
                // Restore service always does work regardless of whether the value we pass them to actually
                // contains changes, only nominate if there are any.
                if (RestoreComparer.RestoreInfos.Equals(_latestValue, restoreInfo))
                    return;

                _latestValue = restoreInfo;

                JoinableTask joinableTask = JoinableFactory.RunAsync(() =>
                {
                    return NominateForRestoreAsync(restoreInfo, _projectAsynchronousTasksService.UnloadCancellationToken);
                });

                _projectAsynchronousTasksService.RegisterAsyncTask(joinableTask,
                                                                   ProjectCriticalOperation.Build | ProjectCriticalOperation.Unload | ProjectCriticalOperation.Rename,
                                                                   registerFaultHandler: true);

                // Prevent overlap until Restore completes
                await joinableTask;
            }

            private async Task NominateForRestoreAsync(ProjectRestoreInfo restoreInfo, CancellationToken cancellationToken)
            {
                RestoreLogger.BeginNominateRestore(_logger, _project.FullPath!, restoreInfo);

                try
                {
                    await _solutionRestoreService.NominateProjectAsync(_project.FullPath, restoreInfo, cancellationToken);
                }
                finally
                {
                    CodeMarkers.Instance.CodeMarker(CodeMarkerTimerId.PerfPackageRestoreEnd);

                    RestoreLogger.EndNominateRestore(_logger, _project.FullPath!);
                }
            }

            private RestoreData CreateRestoreData(ProjectRestoreInfo restoreInfo)
            {
                // Restore service gives us a guarantee that the assets file
                // will contain *at least* the changes that we pushed to it.

                if (restoreInfo.ProjectAssetsFilePath.Length == 0)
                    return new RestoreData(string.Empty, DateTime.MinValue, succeeded: false);

                return new RestoreData(
                    restoreInfo.ProjectAssetsFilePath,
                    GetLastWriteTimeUtc(restoreInfo.ProjectAssetsFilePath));
            }

            private DateTime GetLastWriteTimeUtc(string path)
            {
                Assumes.NotNullOrEmpty(path);

                try
                {
                    return _fileSystem.LastFileWriteTimeUtc(path);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }

                return DateTime.MinValue;
            }
        }
    }
}
