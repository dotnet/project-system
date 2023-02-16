// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Notifications;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;
using Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore.Snapshots;
using Microsoft.VisualStudio.Telemetry;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    public class NuGetRestoreServiceTests
    {
        [Fact]
        public async Task NominateAsyncCallsThroughToNuGetNominate()
        {
            bool nominateCalled = false;

            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Test\Test.csproj");
            var vsNuGetSolutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((path, info, ct) => nominateCalled = true);
            var vsNuGetSolutionRestoreService4 = IVsSolutionRestoreService4Factory.Create();
            var projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
            var faultHandlerService = IProjectFaultHandlerServiceFactory.Create();
            var restoreService = new NuGetRestoreService(project, vsNuGetSolutionRestoreService, vsNuGetSolutionRestoreService4, projectAsynchronousTasksService, faultHandlerService);

            var restoreInfo = ProjectRestoreInfoFactory.Create(msbuildProjectExtensionsPath: @"C:\Alpha\Beta");
            var configuredInputs = PackageRestoreConfiguredInputFactory.Create(restoreInfo);

            var result = await restoreService.NominateAsync(restoreInfo, configuredInputs, default);

            Assert.True(nominateCalled);
        }

        [Fact]
        public async Task UpdateDoesNotCallThroughToNuGetNominate()
        {
            bool nominateCalled = false;

            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Test\Test.csproj");
            var vsNuGetSolutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((path, info, ct) => nominateCalled = true);
            var vsNuGetSolutionRestoreService4 = IVsSolutionRestoreService4Factory.Create();
            var projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
            var faultHandlerService = IProjectFaultHandlerServiceFactory.Create();
            var restoreService = new NuGetRestoreService(project, vsNuGetSolutionRestoreService, vsNuGetSolutionRestoreService4, projectAsynchronousTasksService, faultHandlerService);

            var restoreInfo = ProjectRestoreInfoFactory.Create(msbuildProjectExtensionsPath: @"C:\Alpha\Beta");
            var configuredInputs = PackageRestoreConfiguredInputFactory.Create(restoreInfo);

            await restoreService.UpdateWithoutNominationAsync(configuredInputs);

            Assert.False(nominateCalled);
        }

        [Fact]
        public async Task NominateCausesPendingTaskToComplete()
        {
            IVsProjectRestoreInfoSource? restoreSource = null;
            Task? faultHandlerRegisteredTask = null;
            
            var configuredProject = ConfiguredProjectFactory.Create(projectConfiguration: ProjectConfigurationFactory.Create("Debug|x64"));
            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Test\Test.csproj", configuredProject: configuredProject);
            var vsNuGetSolutionRestoreService = IVsSolutionRestoreServiceFactory.Create();
            var vsNuGetSolutionRestoreService4 = IVsSolutionRestoreService4Factory.ImplementRegisterRestoreInfoSourceAsync((source, ct) => restoreSource = source);
            var projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
            var faultHandlerService = IProjectFaultHandlerServiceFactory.ImplementForget((task, settings, severity, project) => faultHandlerRegisteredTask = task);
            var restoreService = new NuGetRestoreService(project, vsNuGetSolutionRestoreService, vsNuGetSolutionRestoreService4, projectAsynchronousTasksService, faultHandlerService);
            
            await restoreService.LoadAsync();

            Assert.NotNull(faultHandlerRegisteredTask);

            await faultHandlerRegisteredTask;

            Assert.NotNull(restoreSource);

            Task nominationTask = restoreSource.WhenNominated(default);
            Assert.False(nominationTask.IsCompleted);

            var restoreInfo = ProjectRestoreInfoFactory.Create(msbuildProjectExtensionsPath: @"C:\Alpha\Beta");
            var configuredInputs = PackageRestoreConfiguredInputFactory.Create(restoreInfo);

            await restoreService.NominateAsync(restoreInfo, configuredInputs, default);

            Assert.True(nominationTask.IsCompleted);
        }

        [Fact]
        public async Task UpdateCausesPendingTaskToComplete()
        {
            IVsProjectRestoreInfoSource? restoreSource = null;
            Task? faultHandlerRegisteredTask = null;

            var configuredProject = ConfiguredProjectFactory.Create(projectConfiguration: ProjectConfigurationFactory.Create("Debug|x64"));
            var project = UnconfiguredProjectFactory.Create(fullPath: @"C:\Test\Test.csproj", configuredProject: configuredProject);
            var vsNuGetSolutionRestoreService = IVsSolutionRestoreServiceFactory.Create();
            var vsNuGetSolutionRestoreService4 = IVsSolutionRestoreService4Factory.ImplementRegisterRestoreInfoSourceAsync((source, ct) => restoreSource = source);
            var projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
            var faultHandlerService = IProjectFaultHandlerServiceFactory.ImplementForget((task, settings, severity, project) => faultHandlerRegisteredTask = task);
            var restoreService = new NuGetRestoreService(project, vsNuGetSolutionRestoreService, vsNuGetSolutionRestoreService4, projectAsynchronousTasksService, faultHandlerService);

            await restoreService.LoadAsync();

            Assert.NotNull(faultHandlerRegisteredTask);

            await faultHandlerRegisteredTask;

            Assert.NotNull(restoreSource);

            Task nominationTask = restoreSource.WhenNominated(default);
            Assert.False(nominationTask.IsCompleted);

            var restoreInfo = ProjectRestoreInfoFactory.Create(msbuildProjectExtensionsPath: @"C:\Alpha\Beta");
            var configuredInputs = PackageRestoreConfiguredInputFactory.Create(restoreInfo);

            await restoreService.UpdateWithoutNominationAsync(configuredInputs);

            Assert.True(nominationTask.IsCompleted);
        }
    }

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
            bool featureFlagEnabled = false)
        {
            var projectSystemOptionsMock = new Mock<IProjectSystemOptions>();
            projectSystemOptionsMock.Setup(o => o.GetDetectNuGetRestoreCyclesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(featureFlagEnabled);

            var telemetryService = (new Mock<ITelemetryService>()).Object;
            var nonModelNotificationService = (new Mock<INonModalNotificationService>()).Object;
            project ??= UnconfiguredProjectFactory.CreateWithActiveConfiguredProjectProvider(IProjectThreadingServiceFactory.Create());
            dataSource ??= IPackageRestoreUnconfiguredInputDataSourceFactory.Create();
            IProjectAsynchronousTasksService projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
            nuGetRestoreService ??= INuGetRestoreServiceFactory.Create();
            IManagedProjectDiagnosticOutputService logger = IManagedProjectDiagnosticOutputServiceFactory.Create();
            IFileSystem fileSystem = IFileSystemFactory.Create();
            var sharedJoinableTaskCollection = new PackageRestoreSharedJoinableTaskCollection(IProjectThreadingServiceFactory.Create());

            return new PackageRestoreDataSourceMocked(
                projectSystemOptionsMock.Object,
                telemetryService,
                nonModelNotificationService,
                project,
                dataSource,
                projectAsynchronousTasksService,
                fileSystem,
                logger,
                sharedJoinableTaskCollection,
                nuGetRestoreService);
        }
    }
}
