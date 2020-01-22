// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.OperationProgress;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal partial class PackageRestoreProgressTracker
    {
        internal class PackageRestoreProgressTrackerInstance : OnceInitializedOnceDisposed, IMultiLifetimeInstance
        {
            // The steps leading up to and during restore are the following:
            //
            //      1) Evaluation
            //      2) Design-Time build (call CollectPackageReferences, et all)
            //      3) Push package references & restore data to NuGet ("Nominate")
            //      4) If assets file updated during restore, repeat above
            //
            // It can take two cycles of above (during first open, or when assets file is out of 
            // date) before we have a design-time build that contains all the references that a 
            // project depends on, up to and including mscorlib/System.Runtime.
            // 
            // We want the "IntelliSense" operation progress stage to only be considered completed
            // once we've stopped this cycle, which will let Roslyn, designers and other consumers
            // disable commands, or give indicators that the project is still loading.
            //
            // To figure out when we've finished the cycle, we compare the last write time of the 
            // assets file during the last evaluation against the timestamp of the file on disk just 
            // after restore. If they don't match, we know that we're about to repeat the cycle and 
            // we're still incomplete. Once they match in timestamp, we know that the last design-time 
            // build ran with the latest assets file and we notify operation progress that we're now
            // completed with the results.

            private readonly ConfiguredProject _project;
            private readonly IDataProgressTrackerService _dataProgressTrackerService;
            private readonly IPackageRestoreService _restoreService;
            private readonly IProjectSubscriptionService _projectSubscriptionService;

            private IDataProgressTrackerServiceRegistration? _progressRegistration;
            private IDisposable? _subscription;

            public PackageRestoreProgressTrackerInstance(
                ConfiguredProject project,
                IDataProgressTrackerService dataProgressTrackerService,
                IPackageRestoreService restoreService,
                IProjectSubscriptionService projectSubscriptionService)
            {
                _project = project;
                _dataProgressTrackerService = dataProgressTrackerService;
                _restoreService = restoreService;
                _projectSubscriptionService = projectSubscriptionService;
            }

            public Task InitializeAsync()
            {
                EnsureInitialized();

                return Task.CompletedTask;
            }

            protected override void Initialize()
            {
                _progressRegistration = _dataProgressTrackerService.RegisterForIntelliSense(
                    _project,
                    nameof(PackageRestoreProgressTracker));

                Action<IProjectVersionedValue<ValueTuple<IProjectSnapshot, RestoreData>>> action = OnRestoreCompleted;

                _subscription = ProjectDataSources.SyncLinkTo(
                    _projectSubscriptionService.ProjectSource.SourceBlock.SyncLinkOptions(),
                    _restoreService.RestoreData.SyncLinkOptions(),
                        DataflowBlockSlim.CreateActionBlock(action),
                        linkOptions: DataflowOption.PropagateCompletion);
            }

            internal void OnRestoreCompleted(IProjectVersionedValue<ValueTuple<IProjectSnapshot, RestoreData>> value)
            {
                if (IsRestoreUpToDate(value.Value.Item1, value.Value.Item2))
                {
                    _progressRegistration!.NotifyOutputDataCalculated(value.DataSourceVersions);
                }
            }

            private static bool IsRestoreUpToDate(IProjectSnapshot projectSnapshot, RestoreData restoreData)
            {
                // If restore failed, we treat as though it is up-to-date to avoid it forever being stuck out of date.
                if (!restoreData.Succeeded)
                    return true;

                DateTime lastEvaluationWriteTime = GetLastWriteTimeUtc(restoreData.ProjectAssetsFilePath, projectSnapshot);

                return lastEvaluationWriteTime >= restoreData.ProjectAssetsLastWriteTimeUtc;
            }

            private static DateTime GetLastWriteTimeUtc(string filePath, IProjectSnapshot projectSnapshot)
            {
                var projectSnapshot2 = (IProjectSnapshot2)projectSnapshot;

                // If the assets file wasn't included as part of the <AdditionalDesignTimeBuildInput> item,
                // consider it up-to-date by return MaxValue, so that it is not forever stuck out-of-date.
                return projectSnapshot2.AdditionalDependentFileTimes.GetValueOrDefault(filePath, DateTime.MaxValue);
            }

            protected override void Dispose(bool disposing)
            {
                _subscription?.Dispose();
                _progressRegistration?.Dispose();
            }
        }
    }
}
