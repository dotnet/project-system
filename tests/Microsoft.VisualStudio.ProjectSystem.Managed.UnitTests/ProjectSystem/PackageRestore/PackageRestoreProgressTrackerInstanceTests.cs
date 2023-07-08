// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using static Microsoft.VisualStudio.ProjectSystem.PackageRestore.PackageRestoreProgressTracker;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    public class PackageRestoreProgressTrackerInstanceTests
    {
        [Fact]
        public async Task Dispose_WhenNotInitialized_DoesNotThrow()
        {
            var instance = CreateInstance();

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task Dispose_WhenInitialized_DoesNotThrow()
        {
            var instance = await CreateInitializedInstance();

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task OnRestoreCompleted_WhenEvaluationRanWithOlderAssetsFile_IsNotUpToDate()
        {
            string projectAssetsFile = @"C:\Project\obj\project.assets.json";

            var currentTimestamp = DateTime.Now;
            var evaluationTimestamp = DateTime.Now.AddDays(-1);

            var restoreData = new RestoreData(projectAssetsFile, currentTimestamp);
            var snapshot = IProjectSnapshot2Factory.WithAdditionalDependentFileTime(
                projectAssetsFile,
                evaluationTimestamp);

            var result = await OnRestoreCompleted(snapshot, restoreData);

            Assert.False(result);
        }

        [Fact]
        public async Task OnRestoreCompleted_WhenEvaluationRanWithNewerAssetsFile_IsUpToDate()
        {
            string projectAssetsFile = @"C:\Project\obj\project.assets.json";

            var currentTimestamp = DateTime.Now;
            var evaluationTimestamp = DateTime.Now.AddDays(1);

            var restoreData = new RestoreData(projectAssetsFile, currentTimestamp);
            var snapshot = IProjectSnapshot2Factory.WithAdditionalDependentFileTime(
                projectAssetsFile,
                evaluationTimestamp);

            var result = await OnRestoreCompleted(snapshot, restoreData);

            Assert.True(result);
        }

        [Fact]
        public async Task OnRestoreCompleted_WhenEvaluationRanSameAssetsFile_IsUpToDate()
        {
            string projectAssetsFile = @"C:\Project\obj\project.assets.json";

            var currentTimestamp = DateTime.Now;
            var evaluationTimestamp = currentTimestamp;

            var restoreData = new RestoreData(projectAssetsFile, currentTimestamp);
            var snapshot = IProjectSnapshot2Factory.WithAdditionalDependentFileTime(
                projectAssetsFile,
                evaluationTimestamp);

            var result = await OnRestoreCompleted(snapshot, restoreData);

            Assert.True(result);
        }

        [Fact]
        public async Task OnRestoreCompleted_WhenEvaluationIsMissingProjectAssetsFile_IsUpToDate()
        {
            string projectAssetsFile = @"C:\Project\obj\project.assets.json";

            var currentTimestamp = DateTime.Now;

            var restoreData = new RestoreData(projectAssetsFile, currentTimestamp);
            var snapshot = IProjectSnapshot2Factory.Create();

            var result = await OnRestoreCompleted(snapshot, restoreData);

            Assert.True(result);
        }

        [Fact]
        public async Task OnRestoreCompleted_WhenRestoreFailed_IsUpToDate()
        {
            var restoreData = new RestoreData(string.Empty, DateTime.MinValue, succeeded: false);
            var snapshot = IProjectSnapshot2Factory.Create();

            var result = await OnRestoreCompleted(snapshot, restoreData);

            Assert.True(result);
        }

        private async Task<bool> OnRestoreCompleted(IProjectSnapshot projectSnapshot, RestoreData restoreData)
        {
            bool result = false;
            var dataProgressTrackerService = IDataProgressTrackerServiceFactory.ImplementNotifyOutputDataCalculated(_ => { result = true; });

            var instance = await CreateInitializedInstance(dataProgressTrackerService: dataProgressTrackerService);

            var tuple = ValueTuple.Create(projectSnapshot, restoreData);
            var value = IProjectVersionedValueFactory.Create(tuple);

            instance.OnRestoreCompleted(value);

            return result;
        }

        private async Task<PackageRestoreProgressTrackerInstance> CreateInitializedInstance(ConfiguredProject? project = null, IDataProgressTrackerService? dataProgressTrackerService = null, IPackageRestoreDataSource? packageRestoreService = null, IProjectSubscriptionService? projectSubscriptionService = null)
        {
            var instance = CreateInstance(project, dataProgressTrackerService, packageRestoreService, projectSubscriptionService);

            await instance.InitializeAsync();

            return instance;
        }

        private PackageRestoreProgressTrackerInstance CreateInstance(ConfiguredProject? project = null, IDataProgressTrackerService? dataProgressTrackerService = null, IPackageRestoreDataSource? packageRestoreDataSource = null, IProjectSubscriptionService? projectSubscriptionService = null)
        {
            project ??= ConfiguredProjectFactory.Create();
            dataProgressTrackerService ??= IDataProgressTrackerServiceFactory.Create();
            packageRestoreDataSource ??= IPackageRestoreServiceFactory.Create();
            projectSubscriptionService ??= IProjectSubscriptionServiceFactory.Create();

            IProjectThreadingService threadingService = IProjectThreadingServiceFactory.Create();
            IProjectFaultHandlerService projectFaultHandlerService = IProjectFaultHandlerServiceFactory.Create();

            return new PackageRestoreProgressTrackerInstance(
                project,
                threadingService,
                projectFaultHandlerService,
                dataProgressTrackerService,
                packageRestoreDataSource,
                projectSubscriptionService);
        }
    }
}
