// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Utilities.DataFlowExtensions;
using Microsoft.VisualStudio.Shell;
using Moq;
using Xunit;
using Tasks = System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    [ProjectSystemTrait]
    public class StartupProjectRegistrarTests
    {
        [Fact]
        public async Tasks.Task VerifyProjectNotAdded()
        {
            var projectGuid = Guid.NewGuid();

            var mockIVsStartupProjectsListService = IVsStartupProjectsListServiceFactory.CreateMockInstance(projectGuid);
            var iVsStartupProjectsListService = mockIVsStartupProjectsListService.Object;

            var serviceProvider = SVsServiceProviderFactory.Create(iVsStartupProjectsListService);

            var debuggerLaunchProvider = CreateDebuggerLaunchProviderInstance();
            debuggerLaunchProvider.Debuggers.Add(GetLazyDebugLaunchProvider(debugs: false));
            var activeConfiguredProjectWithLaunchProviders = IActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProvider);

            var startupProjectRegistrar = CreateInstance(
                projectGuid,
                serviceProvider,
                activeConfiguredProjectWithLaunchProviders);

            var testWrapperMethod = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapperMock());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.OnProjectFactoryCompletedAsync();

            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Once);
            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Never);
        }

        [Fact]
        public async Tasks.Task VerifyProjectAdded()
        {
            var projectGuid = Guid.NewGuid();

            var mockIVsStartupProjectsListService = IVsStartupProjectsListServiceFactory.CreateMockInstance(projectGuid);
            var iVsStartupProjectsListService = mockIVsStartupProjectsListService.Object;

            var serviceProvider = SVsServiceProviderFactory.Create(iVsStartupProjectsListService);

            var debuggerLaunchProvider = CreateDebuggerLaunchProviderInstance();
            debuggerLaunchProvider.Debuggers.Add(GetLazyDebugLaunchProvider(debugs: true));
            var activeConfiguredProjectWithLaunchProviders = IActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProvider);

            var startupProjectRegistrar = CreateInstance(
                projectGuid,
                serviceProvider,
                activeConfiguredProjectWithLaunchProviders);

            var testWrapperMethod = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapperMock());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.OnProjectFactoryCompletedAsync();

            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Once);
            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Never);
        }

        [Fact]
        public async Tasks.Task VerifyProjectAdded_DifferentProviders()
        {
            var projectGuid = Guid.NewGuid();

            var mockIVsStartupProjectsListService = IVsStartupProjectsListServiceFactory.CreateMockInstance(projectGuid);
            var iVsStartupProjectsListService = mockIVsStartupProjectsListService.Object;

            var serviceProvider = SVsServiceProviderFactory.Create(iVsStartupProjectsListService);

            var debuggerLaunchProvider = CreateDebuggerLaunchProviderInstance();
            debuggerLaunchProvider.Debuggers.Add(GetLazyDebugLaunchProvider(debugs: false));
            debuggerLaunchProvider.Debuggers.Add(GetLazyDebugLaunchProvider(debugs: true));
            var activeConfiguredProjectWithLaunchProviders = IActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProvider);

            var startupProjectRegistrar = CreateInstance(
                projectGuid,
                serviceProvider,
                activeConfiguredProjectWithLaunchProviders);

            var testWrapperMethod = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapperMock());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.OnProjectFactoryCompletedAsync();

            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Once);
            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Never);
        }

        [Fact]
        public async Tasks.Task VerifyProjectAdded_RemovedWithChange()
        {
            var projectGuid = Guid.NewGuid();

            var mockIVsStartupProjectsListService = IVsStartupProjectsListServiceFactory.CreateMockInstance(projectGuid);
            var iVsStartupProjectsListService = mockIVsStartupProjectsListService.Object;

            var serviceProvider = SVsServiceProviderFactory.Create(iVsStartupProjectsListService);

            var debuggerLaunchProvider = CreateDebuggerLaunchProviderInstance();
            debuggerLaunchProvider.Debuggers.Add(GetLazyDebugLaunchProvider(debugs: true));
            var activeConfiguredProjectWithLaunchProviders = IActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProvider);

            var startupProjectRegistrar = CreateInstance(
                projectGuid,
                serviceProvider,
                activeConfiguredProjectWithLaunchProviders);

            var testWrapperMethod = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapperMock());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.OnProjectFactoryCompletedAsync();

            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Once);
            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Never);

            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""OutputType"" ]
            }
        }
    }
}");
            debuggerLaunchProvider.Debuggers.Clear();
            debuggerLaunchProvider.Debuggers.Add(GetLazyDebugLaunchProvider(debugs: false));

            await startupProjectRegistrar.ConfigurationGeneralRuleBlock_ChangedAsync(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(projectSubscriptionUpdate));

            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Once);
        }

        [Fact]
        public async Tasks.Task VerifyProjectRemoved_AddedWithChange()
        {
            var projectGuid = Guid.NewGuid();

            var mockIVsStartupProjectsListService = IVsStartupProjectsListServiceFactory.CreateMockInstance(projectGuid);
            var iVsStartupProjectsListService = mockIVsStartupProjectsListService.Object;

            var serviceProvider = SVsServiceProviderFactory.Create(iVsStartupProjectsListService);

            var debuggerLaunchProvider = CreateDebuggerLaunchProviderInstance();
            debuggerLaunchProvider.Debuggers.Add(GetLazyDebugLaunchProvider(debugs: false));
            var activeConfiguredProjectWithLaunchProviders = IActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProvider);

            var startupProjectRegistrar = CreateInstance(
                projectGuid,
                serviceProvider,
                activeConfiguredProjectWithLaunchProviders);

            var testWrapperMethod = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapperMock());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.OnProjectFactoryCompletedAsync();

            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Never);
            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Once);

            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""OutputType"" ]
            }
        }
    }
}");
            debuggerLaunchProvider.Debuggers.Clear();
            debuggerLaunchProvider.Debuggers.Add(GetLazyDebugLaunchProvider(debugs: true));

            await startupProjectRegistrar.ConfigurationGeneralRuleBlock_ChangedAsync(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(projectSubscriptionUpdate));

            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Once);
        }

        private StartupProjectRegistrar.DebuggerLaunchProviders CreateDebuggerLaunchProviderInstance()
        {
            return new StartupProjectRegistrar.DebuggerLaunchProviders(ConfiguredProjectFactory.Create());
        }

        private Lazy<IDebugLaunchProvider, IDebugLaunchProviderMetadataView> GetLazyDebugLaunchProvider(bool debugs)
        {
            return new Lazy<IDebugLaunchProvider, IDebugLaunchProviderMetadataView>(
                () => IDebugLaunchProviderFactory.ImplementCanLaunchAsync(debugs),
                IDebugLaunchProviderMetadataViewFactory.CreateInstance());
        }

        private StartupProjectRegistrar CreateInstance(
            Guid guid,
            SVsServiceProvider serviceProvider,
            ActiveConfiguredProject<StartupProjectRegistrar.DebuggerLaunchProviders> launchProviders)
        {
            var projectProperties = ProjectPropertiesFactory.Create(IUnconfiguredProjectFactory.Create(), new[] {
                    new PropertyPageData { Category = "ConfigurationGeneral", PropertyName = "ProjectGuid", Value = guid.ToString() }
                });

            return CreateInstance(
                IUnconfiguredProjectVsServicesFactory.Implement(projectProperties: () => projectProperties),
                serviceProvider,
                launchProviders);
        }

        private StartupProjectRegistrar CreateInstance(
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            ActiveConfiguredProject<StartupProjectRegistrar.DebuggerLaunchProviders> launchProviders)
        {
            return new StartupProjectRegistrar(
                projectVsServices,
                serviceProvider,
                IProjectThreadingServiceFactory.Create(),
                IActiveConfiguredProjectSubscriptionServiceFactory.CreateInstance(),
                launchProviders);
        }
    }
}
