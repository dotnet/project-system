// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore.Snapshots;
using Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBarService;
using Microsoft.VisualStudio.Telemetry;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    public class PackageRestoreDataSourceTests
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
            var instance = CreateInitializedInstance();

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task RestoreAsync_PushesRestoreInfoToRestoreService()
        {
            IVsProjectRestoreInfo2? result = null;
            var solutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((projectFile, info, cancellationToken) => { result = info; });

            var instance = CreateInitializedInstance(solutionRestoreService: solutionRestoreService);

            var restoreInfo = ProjectRestoreInfoFactory.Create();
            var ConfigureInputs = PackageRestoreConfiguredInputFactory.Create(restoreInfo);
            var value = IProjectVersionedValueFactory.Create(new PackageRestoreUnconfiguredInput(restoreInfo, ConfigureInputs!));

            await instance.RestoreAsync(value);

            Assert.Same(restoreInfo, result);
        }

        [Fact]
        public async Task RestoreAsync_NullAsRestoreInfo_DoesNotPushToRestoreService()
        {
            int callCount = 0;
            var solutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((projectFile, info, cancellationToken) => { callCount++; });

            var instance = CreateInitializedInstance(solutionRestoreService: solutionRestoreService);

            var value = IProjectVersionedValueFactory.Create(new PackageRestoreUnconfiguredInput(null, new PackageRestoreConfiguredInput[0]));

            await instance.RestoreAsync(value);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task RestoreAsync_UnchangedValueAsValue_DoesNotPushToRestoreService()
        {
            int callCount = 0;
            var solutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((projectFile, info, cancellationToken) => { callCount++; });

            var instance = CreateInitializedInstance(solutionRestoreService: solutionRestoreService);

            var restoreInfo = ProjectRestoreInfoFactory.Create();
            var value = IProjectVersionedValueFactory.Create(new PackageRestoreUnconfiguredInput(restoreInfo, new PackageRestoreConfiguredInput[0]));

            await instance.RestoreAsync(value);

            Assert.Equal(1, callCount); // Should have only been called once
        }

        private static PackageRestoreDataSource CreateInitializedInstance(UnconfiguredProject? project = null, IPackageRestoreUnconfiguredInputDataSource? dataSource = null, IVsSolutionRestoreService3? solutionRestoreService = null)
        {
            var instance = CreateInstance(project, dataSource, solutionRestoreService);
            instance.LoadAsync();

            return instance;
        }

        private static PackageRestoreDataSource CreateInstance(
            UnconfiguredProject? project = null, 
            IPackageRestoreUnconfiguredInputDataSource? dataSource = null, 
            IVsSolutionRestoreService3? solutionRestoreService = null,
            bool featureFlagEnabled = false)
        {
            var featureFlagServiceMock = new Mock<IVsFeatureFlags>();
            featureFlagServiceMock.Setup(m => m.IsFeatureEnabled(FeatureFlagNames.EnableNuGetRestoreCycleDetection, false)).Returns(featureFlagEnabled);
            var vsFeatureFlagsServiceService = new Mock<IVsUIService<SVsFeatureFlags, IVsFeatureFlags>>();
            vsFeatureFlagsServiceService.SetupGet(m => m.Value).Returns(featureFlagServiceMock.Object);

            var telemetryService = (new Mock<ITelemetryService>()).Object;
            var infoBarService = (new Mock<IInfoBarService>()).Object;
            project ??= UnconfiguredProjectFactory.CreateWithActiveConfiguredProjectProvider(IProjectThreadingServiceFactory.Create());
            dataSource ??= IPackageRestoreUnconfiguredInputDataSourceFactory.Create();
            IProjectAsynchronousTasksService projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
            solutionRestoreService ??= IVsSolutionRestoreServiceFactory.Create();
            IManagedProjectDiagnosticOutputService logger = IManagedProjectDiagnosticOutputServiceFactory.Create();
            IFileSystem fileSystem = IFileSystemFactory.Create();
            var vsSolutionRestoreService4 = IVsSolutionRestoreService4Factory.ImplementRegisterRestoreInfoSourceAsync();
            var sharedJoinableTaskCollection = new PackageRestoreSharedJoinableTaskCollection(IProjectThreadingServiceFactory.Create());

            return new PackageRestoreDataSourceMocked(
                vsFeatureFlagsServiceService.Object,
                telemetryService,
                infoBarService,
                project,
                dataSource,
                projectAsynchronousTasksService,
                solutionRestoreService,
                fileSystem,
                logger,
                vsSolutionRestoreService4,
                sharedJoinableTaskCollection);
        }
    }
}
