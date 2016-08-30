using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Xunit;
using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Tasks = System.Threading.Tasks;
using Moq;

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

            var testWrapperMethod = new StartupProjectRegistrar.WrapperMethod(new TestWrapperMethod());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.OnProjectFactoryCompletedAsync();

            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Once);
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

            var testWrapperMethod = new StartupProjectRegistrar.WrapperMethod(new TestWrapperMethod());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.OnProjectFactoryCompletedAsync();

            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Once);
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

            var testWrapperMethod = new StartupProjectRegistrar.WrapperMethod(new TestWrapperMethod());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.OnProjectFactoryCompletedAsync();

            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Once);
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

            var testWrapperMethod = new StartupProjectRegistrar.WrapperMethod(new TestWrapperMethod());
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

            var testWrapperMethod = new StartupProjectRegistrar.WrapperMethod(new TestWrapperMethod());
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
            return new StartupProjectRegistrar.DebuggerLaunchProviders(IConfiguredProjectFactory.Create());
        }

        private Lazy<IDebugLaunchProvider, IDebugLaunchProviderMetadataView> GetLazyDebugLaunchProvider(bool debugs)
        {
            return new Lazy<IDebugLaunchProvider, IDebugLaunchProviderMetadataView>(
                () => IDebugLaunchProviderFactory.CreateInstance(debugs),
                IDebugLaunchProviderMetadataViewFactory.CreateInstance());
        }

        private StartupProjectRegistrar CreateInstance(
            Guid guid,
            SVsServiceProvider serviceProvider,
            ActiveConfiguredProject<StartupProjectRegistrar.DebuggerLaunchProviders> launchProviders)
        {
            var projectProperties = ProjectPropertiesFactory.Create(IUnconfiguredProjectFactory.Create(), new[] {
                    new PropertyPageData { Category = "Project", PropertyName = "ProjectGuid", Value = guid.ToString() }
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
