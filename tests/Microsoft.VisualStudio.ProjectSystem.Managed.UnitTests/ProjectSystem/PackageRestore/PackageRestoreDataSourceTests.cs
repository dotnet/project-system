// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
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
            ProjectRestoreInfo? result = null;
            var nugetRestoreService = INuGetRestoreServiceFactory.ImplementNominateProjectAsync((restoreInfo, versionInfo, cancellationToken) => { result = restoreInfo; });

            var instance = CreateInitializedInstance(nugetRestoreService: nugetRestoreService);

            var restoreInfo = ProjectRestoreInfoFactory.Create(msbuildProjectExtensionsPath: @"C:\Alpha\Beta");
            var ConfigureInputs = PackageRestoreConfiguredInputFactory.Create(restoreInfo);
            var value = IProjectVersionedValueFactory.Create(new PackageRestoreUnconfiguredInput(restoreInfo, ConfigureInputs!));

            await instance.RestoreAsync(value);

            Assert.NotNull(result);
            Assert.Equal(expected: restoreInfo.MSBuildProjectExtensionsPath, actual: result.MSBuildProjectExtensionsPath);
        }

        [Fact]
        public async Task RestoreAsync_NullAsRestoreInfo_DoesNotPushToRestoreService()
        {
            int callCount = 0;
            var nugetRestoreService = INuGetRestoreServiceFactory.ImplementNominateProjectAsync((restoreInfo, versionInfo, cancellationToken) => { callCount++; });

            var instance = CreateInitializedInstance(nugetRestoreService: nugetRestoreService);

            var value = IProjectVersionedValueFactory.Create(new PackageRestoreUnconfiguredInput(null, new PackageRestoreConfiguredInput[0]));

            await instance.RestoreAsync(value);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task RestoreAsync_UnchangedValueAsValue_DoesNotPushToRestoreService()
        {
            int callCount = 0;
            var nugetRestoreService = INuGetRestoreServiceFactory.ImplementNominateProjectAsync((restoreInfo, versionInfo, cancellationToken) => { callCount++; });

            var instance = CreateInitializedInstance(nugetRestoreService: nugetRestoreService);

            var restoreInfo = ProjectRestoreInfoFactory.Create();
            var value = IProjectVersionedValueFactory.Create(new PackageRestoreUnconfiguredInput(restoreInfo, new PackageRestoreConfiguredInput[0]));

            await instance.RestoreAsync(value);

            Assert.Equal(1, callCount); // Should have only been called once
        }

        private static PackageRestoreDataSource CreateInitializedInstance(UnconfiguredProject? project = null, IPackageRestoreUnconfiguredInputDataSource? dataSource = null, INuGetRestoreService? nugetRestoreService = null)
        {
            var instance = CreateInstance(project, dataSource, nugetRestoreService);
            instance.LoadAsync();

            return instance;
        }

        private static PackageRestoreDataSource CreateInstance(
            UnconfiguredProject? project = null,
            IPackageRestoreUnconfiguredInputDataSource? dataSource = null,
            INuGetRestoreService? nuGetRestoreService = null,
            bool featureFlagEnabled = false,
            bool isCycleDetected = false)
        {
            project ??= UnconfiguredProjectFactory.CreateWithActiveConfiguredProjectProvider(IProjectThreadingServiceFactory.Create());
            var sharedJoinableTaskCollection = new PackageRestoreSharedJoinableTaskCollection(IProjectThreadingServiceFactory.Create());

            dataSource ??= IPackageRestoreUnconfiguredInputDataSourceFactory.Create();
            IProjectAsynchronousTasksService projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
            IFileSystem fileSystem = IFileSystemFactory.Create();
            IManagedProjectDiagnosticOutputService logger = IManagedProjectDiagnosticOutputServiceFactory.Create();
            nuGetRestoreService ??= INuGetRestoreServiceFactory.Create();

            var cycleDetector = new Mock<IPackageRestoreCycleDetector>();
            cycleDetector.Setup(o => o.IsCycleDetectedAsync(It.IsAny<Hash>(), It.IsAny<CancellationToken>())).ReturnsAsync(isCycleDetected);

            return new PackageRestoreDataSourceMocked(
                project,
                sharedJoinableTaskCollection,
                dataSource,
                projectAsynchronousTasksService,
                fileSystem,
                logger,
                nuGetRestoreService,
                cycleDetector.Object);
        }
    }
}
