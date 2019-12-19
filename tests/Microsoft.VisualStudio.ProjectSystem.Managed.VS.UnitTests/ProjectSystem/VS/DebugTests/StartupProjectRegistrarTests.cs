// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;
using static Microsoft.VisualStudio.ProjectSystem.VS.Debug.StartupProjectRegistrar;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    public class StartupProjectRegistrarTests
    {
        [Fact]
        public void Dispose_WhenNotInitialized_DoesNotThrow()
        {
            var registrar = CreateInstance();

            registrar.Dispose();

            Assert.True(registrar.IsDisposed);
        }

        [Fact]
        public async Task Disposed_WhenInitialized_DoesNotThrow()
        {
            var registrar = await CreateInitializedInstanceAsync();

            registrar.Dispose();

            Assert.True(registrar.IsDisposed);
        }

        [Fact]
        public async Task OnProjectChanged_WhenNotDebuggable_ProjectNotRegistered()
        {
            var vsStartupProjectsListService = new VsStartupProjectsListService();
            var debugProvider = IDebugLaunchProviderFactory.ImplementCanLaunchAsync(() => false);
            var registrar = await CreateInitializedInstanceAsync(vsStartupProjectsListService, debugProvider);

            await registrar.OnProjectChangedAsync();

            Assert.Null(vsStartupProjectsListService.ProjectGuid);
        }

        [Fact]
        public async Task OnProjectChanged_WhenDebuggable_ProjectRegistered()
        {
            var projectGuid = Guid.NewGuid();
            var vsStartupProjectsListService = new VsStartupProjectsListService();
            var debugProvider = IDebugLaunchProviderFactory.ImplementCanLaunchAsync(() => true);
            var registrar = await CreateInitializedInstanceAsync(projectGuid, vsStartupProjectsListService, debugProvider);

            await registrar.OnProjectChangedAsync();

            Assert.Equal(projectGuid, vsStartupProjectsListService.ProjectGuid);
        }

        [Fact]
        public async Task OnProjectChanged_WhenProjectAlreadyRegisteredAndDebuggable_RemainsRegistered()
        {
            var projectGuid = Guid.NewGuid();
            var vsStartupProjectsListService = new VsStartupProjectsListService();
            var debugProvider = IDebugLaunchProviderFactory.ImplementCanLaunchAsync(() => true);
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
            var debugProvider = IDebugLaunchProviderFactory.ImplementCanLaunchAsync(() => false);
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
            var debugProvider = IDebugLaunchProviderFactory.ImplementCanLaunchAsync(() => isDebuggable);
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
            var debugProvider = IDebugLaunchProviderFactory.ImplementCanLaunchAsync(() => isDebuggable);
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
            var debugProvider1 = IDebugLaunchProviderFactory.ImplementCanLaunchAsync(() => false);
            var debugProvider2 = IDebugLaunchProviderFactory.ImplementCanLaunchAsync(() => true);

            var registrar = await CreateInitializedInstanceAsync(projectGuid, vsStartupProjectsListService, debugProvider1, debugProvider2);

            await registrar.OnProjectChangedAsync();

            Assert.Equal(projectGuid, vsStartupProjectsListService.ProjectGuid);
        }

        private static Task<StartupProjectRegistrar> CreateInitializedInstanceAsync(IVsStartupProjectsListService vsStartupProjectsListService, params IDebugLaunchProvider[] launchProviders)
        {
            return CreateInitializedInstanceAsync(Guid.NewGuid(), vsStartupProjectsListService, launchProviders);
        }

        private static Task<StartupProjectRegistrar> CreateInitializedInstanceAsync(Guid projectGuid, IVsStartupProjectsListService vsStartupProjectsListService, params IDebugLaunchProvider[] launchProviders)
        {
            var projectGuidService = ISafeProjectGuidServiceFactory.ImplementGetProjectGuidAsync(projectGuid);
            var debuggerLaunchProviders = new DebuggerLaunchProviders(ConfiguredProjectFactory.Create());

            int orderPrecedence = 0;
            foreach (IDebugLaunchProvider launchProvider in launchProviders)
            {
                debuggerLaunchProviders.Debuggers.Add(launchProvider, orderPrecedence: orderPrecedence++);
            }

            return CreateInitializedInstanceAsync(vsStartupProjectsListService: vsStartupProjectsListService,
                                                  projectGuidService: projectGuidService,
                                                  launchProviders: ActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProviders));
        }

        private static async Task<StartupProjectRegistrar> CreateInitializedInstanceAsync(
           IVsStartupProjectsListService? vsStartupProjectsListService = null,
           IProjectThreadingService? threadingService = null,
           ISafeProjectGuidService? projectGuidService = null,
           IActiveConfiguredProjectSubscriptionService? projectSubscriptionService = null,
           ActiveConfiguredProject<DebuggerLaunchProviders>? launchProviders = null)
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
            ActiveConfiguredProject<DebuggerLaunchProviders>? launchProviders = null)
        {
            var instance = new StartupProjectRegistrar(
                null!,
                IVsServiceFactory.Create<SVsStartupProjectsListService, IVsStartupProjectsListService>(vsStartupProjectsListService),
                threadingService ?? IProjectThreadingServiceFactory.Create(),
                projectGuidService ?? ISafeProjectGuidServiceFactory.ImplementGetProjectGuidAsync(Guid.NewGuid()),
                projectSubscriptionService ?? IActiveConfiguredProjectSubscriptionServiceFactory.Create(),
                launchProviders!);

            return instance;
        }
    }
}
