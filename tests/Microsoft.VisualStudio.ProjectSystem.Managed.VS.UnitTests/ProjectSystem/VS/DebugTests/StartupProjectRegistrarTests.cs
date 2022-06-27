// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    public class StartupProjectRegistrarTests
    {
        [Fact]
        public async Task DisposeAsync_WhenNotInitialized_DoesNotThrow()
        {
            var registrar = CreateInstance();

            await registrar.DisposeAsync();

            Assert.True(registrar.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_WhenInitialized_DoesNotThrow()
        {
            var registrar = await CreateInitializedInstanceAsync();

            await registrar.DisposeAsync();

            Assert.True(registrar.IsDisposed);
        }

        [Fact]
        public async Task OnProjectChanged_WhenNotDebuggable_ProjectNotRegistered()
        {
            var vsStartupProjectsListService = new VsStartupProjectsListService();
            var debugProvider = IDebugLaunchProviderFactory.ImplementIsProjectDebuggableAsync(() => false);
            var registrar = await CreateInitializedInstanceAsync(vsStartupProjectsListService, debugProvider);

            await registrar.OnProjectChangedAsync();

            Assert.Null(vsStartupProjectsListService.ProjectGuid);
        }

        [Fact]
        public async Task OnProjectChanged_WhenDebuggable_ProjectRegistered()
        {
            var projectGuid = Guid.NewGuid();
            var vsStartupProjectsListService = new VsStartupProjectsListService();
            var debugProvider = IDebugLaunchProviderFactory.ImplementIsProjectDebuggableAsync(() => true);
            var registrar = await CreateInitializedInstanceAsync(projectGuid, vsStartupProjectsListService, debugProvider);

            await registrar.OnProjectChangedAsync();

            Assert.Equal(projectGuid, vsStartupProjectsListService.ProjectGuid);
        }

        [Fact]
        public async Task OnProjectChanged_WhenProjectAlreadyRegisteredAndDebuggable_RemainsRegistered()
        {
            var projectGuid = Guid.NewGuid();
            var vsStartupProjectsListService = new VsStartupProjectsListService();
            var debugProvider = IDebugLaunchProviderFactory.ImplementIsProjectDebuggableAsync(() => true);
            var registrar = await CreateInitializedInstanceAsync(projectGuid, vsStartupProjectsListService, debugProvider);

            await registrar.OnProjectChangedAsync();

            Assert.Equal(projectGuid, vsStartupProjectsListService.ProjectGuid);

            await registrar.OnProjectChangedAsync();

            Assert.Equal(projectGuid, vsStartupProjectsListService.ProjectGuid);
        }

        [Fact]
        public async Task OnProjectChanged_WhenProjectNotRegisteredAndNotDebuggable_RemainsUnregistered()
        {
            var vsStartupProjectsListService = new VsStartupProjectsListService();
            var debugProvider = IDebugLaunchProviderFactory.ImplementIsProjectDebuggableAsync(() => false);
            var registrar = await CreateInitializedInstanceAsync(vsStartupProjectsListService, debugProvider);

            await registrar.OnProjectChangedAsync();

            Assert.Null(vsStartupProjectsListService.ProjectGuid);

            await registrar.OnProjectChangedAsync();

            Assert.Null(vsStartupProjectsListService.ProjectGuid);
        }

        [Fact]
        public async Task OnProjectChanged_WhenProjectAlreadyRegisteredAndNotDebuggable_ProjectUnregistered()
        {
            bool isDebuggable = true;
            var projectGuid = Guid.NewGuid();
            var vsStartupProjectsListService = new VsStartupProjectsListService();
            var debugProvider = IDebugLaunchProviderFactory.ImplementIsProjectDebuggableAsync(() => isDebuggable);
            var registrar = await CreateInitializedInstanceAsync(projectGuid, vsStartupProjectsListService, debugProvider);

            await registrar.OnProjectChangedAsync();

            isDebuggable = false;

            await registrar.OnProjectChangedAsync();

            Assert.Null(vsStartupProjectsListService.ProjectGuid);
        }

        [Fact]
        public async Task OnProjectChanged_WhenProjectNotRegisteredAndDebuggable_ProjectRegistered()
        {
            bool isDebuggable = false;
            var projectGuid = Guid.NewGuid();
            var vsStartupProjectsListService = new VsStartupProjectsListService();
            var debugProvider = IDebugLaunchProviderFactory.ImplementIsProjectDebuggableAsync(() => isDebuggable);
            var registrar = await CreateInitializedInstanceAsync(projectGuid, vsStartupProjectsListService, debugProvider);

            await registrar.OnProjectChangedAsync();

            isDebuggable = true;

            await registrar.OnProjectChangedAsync();

            Assert.Equal(projectGuid, vsStartupProjectsListService.ProjectGuid);
        }

        [Fact]
        public async Task OnProjectChanged_ConsultsAllDebugProviders()
        {
            var projectGuid = Guid.NewGuid();
            var vsStartupProjectsListService = new VsStartupProjectsListService();
            var debugProvider1 = IDebugLaunchProviderFactory.ImplementIsProjectDebuggableAsync(() => false);
            var debugProvider2 = IDebugLaunchProviderFactory.ImplementIsProjectDebuggableAsync(() => true);

            var registrar = await CreateInitializedInstanceAsync(projectGuid, vsStartupProjectsListService, debugProvider1, debugProvider2);

            await registrar.OnProjectChangedAsync();

            Assert.Equal(projectGuid, vsStartupProjectsListService.ProjectGuid);
        }

        [Fact]
        public async Task OnProjectChanged_VsStartupProjectsListServiceIsNull_DoesNotThrow()
        {
            var debugProvider = IDebugLaunchProviderFactory.ImplementIsProjectDebuggableAsync(() => false);
            var registrar = await CreateInitializedInstanceAsync(vsStartupProjectsListService: null, debugProvider);

            await registrar.OnProjectChangedAsync();
        }

        private static Task<StartupProjectRegistrar> CreateInitializedInstanceAsync(IVsStartupProjectsListService? vsStartupProjectsListService, params IDebugLaunchProvider[] launchProviders)
        {
            return CreateInitializedInstanceAsync(Guid.NewGuid(), vsStartupProjectsListService, launchProviders);
        }

        private static Task<StartupProjectRegistrar> CreateInitializedInstanceAsync(Guid projectGuid, IVsStartupProjectsListService? vsStartupProjectsListService, params IDebugLaunchProvider[] launchProviders)
        {
            var projectGuidService = ISafeProjectGuidServiceFactory.ImplementGetProjectGuidAsync(projectGuid);
            var debuggerLaunchProviders = new OrderPrecedenceImportCollection<IDebugLaunchProvider>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst);

            int orderPrecedence = 0;
            foreach (IDebugLaunchProvider launchProvider in launchProviders)
            {
                debuggerLaunchProviders.Add(launchProvider, orderPrecedence: orderPrecedence++);
            }

            return CreateInitializedInstanceAsync(vsStartupProjectsListService: vsStartupProjectsListService,
                                                  projectGuidService: projectGuidService,
                                                  launchProviders: IActiveConfiguredValuesFactory.ImplementValues(() => debuggerLaunchProviders));
        }

        private static async Task<StartupProjectRegistrar> CreateInitializedInstanceAsync(
           IVsStartupProjectsListService? vsStartupProjectsListService = null,
           IProjectThreadingService? threadingService = null,
           ISafeProjectGuidService? projectGuidService = null,
           IActiveConfiguredProjectSubscriptionService? projectSubscriptionService = null,
           IActiveConfiguredValues<IDebugLaunchProvider>? launchProviders = null)
        {
            var instance = CreateInstance(vsStartupProjectsListService, threadingService, projectGuidService, projectSubscriptionService, launchProviders);
            await instance.InitializeAsync();

            return instance;
        }

        private static StartupProjectRegistrar CreateInstance(
            IVsStartupProjectsListService? vsStartupProjectsListService = null,
            IProjectThreadingService? threadingService = null,
            ISafeProjectGuidService? projectGuidService = null,
            IActiveConfiguredProjectSubscriptionService? projectSubscriptionService = null,
            IActiveConfiguredValues<IDebugLaunchProvider>? launchProviders = null)
        {
            var project = UnconfiguredProjectFactory.Create();
            var instance = new StartupProjectRegistrar(
                project,
                IUnconfiguredProjectTasksServiceFactory.Create(),
                IVsServiceFactory.Create<SVsStartupProjectsListService, IVsStartupProjectsListService>(vsStartupProjectsListService!),
                threadingService ?? IProjectThreadingServiceFactory.Create(),
                projectGuidService ?? ISafeProjectGuidServiceFactory.ImplementGetProjectGuidAsync(Guid.NewGuid()),
                projectSubscriptionService ?? IActiveConfiguredProjectSubscriptionServiceFactory.Create(),
                launchProviders!);

            return instance;
        }
    }
}
