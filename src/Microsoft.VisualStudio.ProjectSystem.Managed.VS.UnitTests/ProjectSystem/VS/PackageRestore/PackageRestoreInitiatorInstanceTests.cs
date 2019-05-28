// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Logging;

using NuGet.SolutionRestoreManager;

using Xunit;

using static Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore.PackageRestoreInitiator;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    public class PackageRestoreInitiatorInstanceTests
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
        public async Task OnRestoreInfoChangedAsync_PushesRestoreInfoToRestoreService()
        {
            IVsProjectRestoreInfo2 result = null;
            var solutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((projectFile, info, cancellationToken) => { result = info; });

            var instance = await CreateInitializedInstance(solutionRestoreService: solutionRestoreService);

            var restoreInfo = IVsProjectRestoreInfo2Factory.Create();
            var value = IProjectVersionedValueFactory.Create(restoreInfo);

            await instance.OnRestoreInfoChangedAsync(value);

            Assert.Same(restoreInfo, result);
        }

        [Fact]
        public async Task OnRestoreInfoChangedAsync_NullAsValue_DoesNotPushToRestoreService()
        {
            int callCount = 0;
            var solutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((projectFile, info, cancellationToken) => { callCount++; });

            var instance = await CreateInitializedInstance(solutionRestoreService: solutionRestoreService);

            var value = IProjectVersionedValueFactory.Create<IVsProjectRestoreInfo2>(null);

            await instance.OnRestoreInfoChangedAsync(value);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task OnRestoreInfoChangedAsync_UnchangedValueAsValue_DoesNotPushToRestoreService()
        {
            int callCount = 0;
            var solutionRestoreService = IVsSolutionRestoreServiceFactory.ImplementNominateProjectAsync((projectFile, info, cancellationToken) => { callCount++; });

            var instance = await CreateInitializedInstance(solutionRestoreService: solutionRestoreService);

            var restoreInfo = IVsProjectRestoreInfo2Factory.Create();
            var value = IProjectVersionedValueFactory.Create<IVsProjectRestoreInfo2>(restoreInfo);

            await instance.OnRestoreInfoChangedAsync(value);

            Assert.Equal(1, callCount); // Should have only been called once
        }

        private async Task<PackageRestoreInitiatorInstance> CreateInitializedInstance(UnconfiguredProject project = null, IPackageRestoreUnconfiguredDataSource dataSource = null, IVsSolutionRestoreService3 solutionRestoreService = null)
        {
            var instance = CreateInstance(project, dataSource, solutionRestoreService);

            await instance.InitializeAsync();

            return instance;
        }

        private PackageRestoreInitiatorInstance CreateInstance(UnconfiguredProject project = null, IPackageRestoreUnconfiguredDataSource dataSource = null, IVsSolutionRestoreService3 solutionRestoreService = null)
        {
            project ??= UnconfiguredProjectFactory.Create();
            dataSource ??= IPackageRestoreUnconfiguredDataSourceFactory.Create();
            IProjectThreadingService threadingService = IProjectThreadingServiceFactory.Create();
            IProjectAsynchronousTasksService projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
            solutionRestoreService ??= IVsSolutionRestoreServiceFactory.Create();
            IProjectLogger logger = IProjectLoggerFactory.Create();

            return new PackageRestoreInitiatorInstance(project, dataSource, threadingService, projectAsynchronousTasksService, solutionRestoreService, logger);
        }
    }
}
