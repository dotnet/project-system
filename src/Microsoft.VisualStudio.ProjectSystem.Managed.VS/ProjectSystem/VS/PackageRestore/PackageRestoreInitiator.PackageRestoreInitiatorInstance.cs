// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Internal.Performance;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Microsoft.VisualStudio.Threading;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal partial class PackageRestoreInitiator
    {
        internal class PackageRestoreInitiatorInstance : OnceInitializedOnceDisposedAsync, IMultiLifetimeInstance
        {
            private readonly UnconfiguredProject _project;
            private readonly IPackageRestoreUnconfiguredDataSource _dataSource;
            private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;
            private readonly IVsSolutionRestoreService3 _solutionRestoreService;
            private readonly IProjectLogger _logger;

            private IDisposable _subscription;
            private IVsProjectRestoreInfo2 _latestValue;

            public PackageRestoreInitiatorInstance(
                UnconfiguredProject project,
                IPackageRestoreUnconfiguredDataSource dataSource,
                IProjectThreadingService threadingService,
                IProjectAsynchronousTasksService projectAsynchronousTasksService,
                IVsSolutionRestoreService3 solutionRestoreService,
                IProjectLogger logger)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _dataSource = dataSource;
                _projectAsynchronousTasksService = projectAsynchronousTasksService;
                _solutionRestoreService = solutionRestoreService;
                _logger = logger;
            }

            public Task InitializeAsync()
            {
                return InitializeAsync(CancellationToken.None);
            }

            protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                _subscription = _dataSource.SourceBlock.LinkToAsyncAction(OnRestoreInfoChangedAsync);

                return Task.CompletedTask;
            }

            protected override Task DisposeCoreAsync(bool initialized)
            {
                _subscription?.Dispose();

                return Task.CompletedTask;
            }

            internal async Task OnRestoreInfoChangedAsync(IProjectVersionedValue<IVsProjectRestoreInfo2> e)
            {
                // Restore service always does work regardless of whether the value we pass them to actually
                // contains changes, only nominate if there are any.
                if (RestoreComparer.RestoreInfos.Equals(_latestValue, e.Value))
                    return;

                // No configurations - likely during project close
                if (e.Value == null)
                    return;

                _latestValue = e.Value;

                JoinableTask joinableTask = JoinableFactory.RunAsync(() =>
                {
                    return NominateProjectRestoreAsync(e.Value, _projectAsynchronousTasksService.UnloadCancellationToken);
                });

                _projectAsynchronousTasksService.RegisterAsyncTask(joinableTask,
                                                                   ProjectCriticalOperation.Build | ProjectCriticalOperation.Unload | ProjectCriticalOperation.Rename,
                                                                   registerFaultHandler: true);

                // Prevent overlap until Restore completes
                await joinableTask;
            }

            private async Task NominateProjectRestoreAsync(IVsProjectRestoreInfo2 restoreInfo, CancellationToken cancellationToken)
            {
                RestoreLogger.BeginNominateRestore(_logger, _project.FullPath, restoreInfo);

                // Nominate NuGet with the restore data. This will complete when we're guaranteed 
                // that the  assets files *at least* contains the changes that we pushed to it.
                await _solutionRestoreService.NominateProjectAsync(_project.FullPath, restoreInfo, cancellationToken);

                CodeMarkers.Instance.CodeMarker(CodeMarkerTimerId.PerfPackageRestoreEnd);

                RestoreLogger.EndNominateRestore(_logger, _project.FullPath);
            }
        }
    }
}
