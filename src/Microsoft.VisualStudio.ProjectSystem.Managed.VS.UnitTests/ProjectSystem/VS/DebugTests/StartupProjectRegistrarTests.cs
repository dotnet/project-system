// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Utilities.DataFlowExtensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
            var activeConfiguredProjectWithLaunchProviders = ActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProvider);

            var startupProjectRegistrar = CreateInstance(
                serviceProvider,
                activeConfiguredProjectWithLaunchProviders);

            var testWrapperMethod = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapperMock());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.Load();

            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""Something"" ]
            }
        }
    }
}");
            await startupProjectRegistrar.ConfigurationGeneralRuleBlock_ChangedAsync(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(projectSubscriptionUpdate));

            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Never);
            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Never);

            projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""ProjectGuid"" ]
            },
            ""After"": {
                ""Properties"": {
                   ""ProjectGuid"": ""GuidNotParsable""
                }
            }
        }
    }
}");
            await startupProjectRegistrar.ConfigurationGeneralRuleBlock_ChangedAsync(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(projectSubscriptionUpdate));

            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Never);
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
            var activeConfiguredProjectWithLaunchProviders = ActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProvider);

            var startupProjectRegistrar = CreateInstance(
                serviceProvider,
                activeConfiguredProjectWithLaunchProviders);

            var testWrapperMethod = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapperMock());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.Load();


            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""ProjectGuid"" ]
            },
            ""After"": {
                ""Properties"": {
                   ""ProjectGuid"": """ + projectGuid + @"""
                }
            }
        }
    }
}");
            await startupProjectRegistrar.ConfigurationGeneralRuleBlock_ChangedAsync(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(projectSubscriptionUpdate));

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
            var activeConfiguredProjectWithLaunchProviders = ActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProvider);

            var startupProjectRegistrar = CreateInstance(
                serviceProvider,
                activeConfiguredProjectWithLaunchProviders);

            var testWrapperMethod = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapperMock());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.Load();


            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""ProjectGuid"" ]
            },
            ""After"": {
                ""Properties"": {
                   ""ProjectGuid"": """ + projectGuid + @"""
                }
            }
        }
    }
}");
            await startupProjectRegistrar.ConfigurationGeneralRuleBlock_ChangedAsync(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(projectSubscriptionUpdate));

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
            var activeConfiguredProjectWithLaunchProviders = ActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProvider);

            var startupProjectRegistrar = CreateInstance(
                serviceProvider,
                activeConfiguredProjectWithLaunchProviders);

            var testWrapperMethod = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapperMock());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.Load();


            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""ProjectGuid"" ]
            },
            ""After"": {
                ""Properties"": {
                   ""ProjectGuid"": """ + projectGuid + @"""
                }
            }
        }
    }
}");
            await startupProjectRegistrar.ConfigurationGeneralRuleBlock_ChangedAsync(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(projectSubscriptionUpdate));

            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Once);
            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Never);

            debuggerLaunchProvider.Debuggers.Clear();
            debuggerLaunchProvider.Debuggers.Add(GetLazyDebugLaunchProvider(debugs: false));

            projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""OutputType"" ]
            }
        }
    }
}");

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
            var activeConfiguredProjectWithLaunchProviders = ActiveConfiguredProjectFactory.ImplementValue(() => debuggerLaunchProvider);

            var startupProjectRegistrar = CreateInstance(
                serviceProvider,
                activeConfiguredProjectWithLaunchProviders);

            var testWrapperMethod = new DataFlowExtensionMethodCaller(new DataFlowExtensionMethodWrapperMock());
            startupProjectRegistrar.WrapperMethodCaller = testWrapperMethod;

            await startupProjectRegistrar.Load();


            var projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""ProjectGuid"" ]
            },
            ""After"": {
                ""Properties"": {
                   ""ProjectGuid"": """ + projectGuid + @"""
                }
            }
        }
    }
}");
            await startupProjectRegistrar.ConfigurationGeneralRuleBlock_ChangedAsync(
                IProjectVersionedValueFactory<IProjectSubscriptionUpdate>.Create(projectSubscriptionUpdate));

            mockIVsStartupProjectsListService.Verify(s => s.AddProject(ref projectGuid), Times.Never);
            mockIVsStartupProjectsListService.Verify(s => s.RemoveProject(ref projectGuid), Times.Once);

            debuggerLaunchProvider.Debuggers.Clear();
            debuggerLaunchProvider.Debuggers.Add(GetLazyDebugLaunchProvider(debugs: true));

            projectSubscriptionUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""ProjectChanges"": {
        ""ConfigurationGeneral"": {
            ""Difference"": {
                ""ChangedProperties"": [ ""OutputType"" ]
            }
        }
    }
}");

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
            SVsServiceProvider serviceProvider,
            ActiveConfiguredProject<StartupProjectRegistrar.DebuggerLaunchProviders> launchProviders)
        {
            return new StartupProjectRegistrar(
                serviceProvider,
                IProjectThreadingServiceFactory.Create(),
                IActiveConfiguredProjectSubscriptionServiceFactory.CreateInstance(),
                launchProviders);
        }
    }
}
