// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using NuGet.SolutionRestoreManager;
using Xunit;
using static Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore.PackageRestoreService;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    public class PackageRestoreServiceInstanceTests
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
        public async Task OnInputsChangedAsync_PushesRestoreInfoToRestoreService()
        {
            IVsProjectRestoreInfo2? result = null;
            var solutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((projectFile, info, cancellationToken) => { result = info; });

            var instance = await CreateInitializedInstance(solutionRestoreService: solutionRestoreService);

            var restoreInfo = ProjectRestoreInfoFactory.Create();
            var value = IProjectVersionedValueFactory.Create(new PackageRestoreUnconfiguredInput(restoreInfo, new PackageRestoreConfiguredInput[0]));

            await instance.OnInputsChangedAsync(value);

            Assert.Same(restoreInfo, result);
        }

        [Fact]
        public async Task OnInputsChangedAsync_NullAsRestoreInfo_DoesNotPushToRestoreService()
        {
            int callCount = 0;
            var solutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((projectFile, info, cancellationToken) => { callCount++; });

            var instance = await CreateInitializedInstance(solutionRestoreService: solutionRestoreService);

            var value = IProjectVersionedValueFactory.Create(new PackageRestoreUnconfiguredInput(null, new PackageRestoreConfiguredInput[0]));

            await instance.OnInputsChangedAsync(value);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task OnInputsChangedAsync_UnchangedValueAsValue_DoesNotPushToRestoreService()
        {
            int callCount = 0;
            var solutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((projectFile, info, cancellationToken) => { callCount++; });

            var instance = await CreateInitializedInstance(solutionRestoreService: solutionRestoreService);

            var restoreInfo = ProjectRestoreInfoFactory.Create();
            var value = IProjectVersionedValueFactory.Create(new PackageRestoreUnconfiguredInput(restoreInfo, new PackageRestoreConfiguredInput[0]));

            await instance.OnInputsChangedAsync(value);

            Assert.Equal(1, callCount); // Should have only been called once
        }

        private async Task<PackageRestoreServiceInstance> CreateInitializedInstance(UnconfiguredProject? project = null, IPackageRestoreUnconfiguredInputDataSource? dataSource = null, IVsSolutionRestoreService3? solutionRestoreService = null)
        {
            var instance = CreateInstance(project, dataSource, solutionRestoreService);

            await instance.InitializeAsync();

            return instance;
        }

        private PackageRestoreServiceInstance CreateInstance(UnconfiguredProject? project = null, IPackageRestoreUnconfiguredInputDataSource? dataSource = null, IVsSolutionRestoreService3? solutionRestoreService = null)
        {
            project ??= UnconfiguredProjectFactory.Create();
            dataSource ??= IPackageRestoreUnconfiguredInputDataSourceFactory.Create();
            IProjectThreadingService threadingService = IProjectThreadingServiceFactory.Create();
            IProjectAsynchronousTasksService projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
            solutionRestoreService ??= IVsSolutionRestoreServiceFactory.Create();
            IProjectLogger logger = IProjectLoggerFactory.Create();
            IFileSystem fileSystem = IFileSystemFactory.Create();
            var broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<RestoreData>>();

            return new PackageRestoreServiceInstance(
                project,
                dataSource,
                threadingService,
                projectAsynchronousTasksService,
                solutionRestoreService,
                fileSystem,
                logger,
                broadcastBlock);
        }
    }
}
