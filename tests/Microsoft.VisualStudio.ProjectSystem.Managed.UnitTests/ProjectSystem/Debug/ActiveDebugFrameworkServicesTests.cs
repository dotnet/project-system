// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Input.Commands
{
    public class ActiveDebugFrameworkServicesTests
    {
        [Theory]
        [InlineData("netcoreapp1.0;net462", new string[] { "netcoreapp1.0", "net462" })]
        [InlineData("net461;netcoreapp1.0;net45;net462", new string[] { "net461", "netcoreapp1.0", "net45", "net462" })]
        public async Task GetProjectFrameworksAsync_ReturnsFrameworksInCorrectOrder(string frameworks, string[] expectedOrder)
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworksProperty, frameworks);

            var projectProperties = ProjectPropertiesFactory.Create(project, data);

            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var debugFrameworkSvcs = new ActiveDebugFrameworkServices(null!, commonServices);
            var result = await debugFrameworkSvcs.GetProjectFrameworksAsync();

            Assert.NotNull(result);
            Assert.Equal(expectedOrder.Length, result.Count);
            for (int i = 0; i < result.Count; i++)
            {
                Assert.Equal(expectedOrder[i], result[i]);
            }
        }

        [Theory]
        [InlineData("netcoreapp1.0")]
        [InlineData("net461")]
        public async Task GetActiveDebuggingFrameworkPropertyAsync_ReturnsFrameworkValue(string framework)
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ProjectDebugger.SchemaName, ProjectDebugger.ActiveDebugFrameworkProperty, framework);

            var projectProperties = ProjectPropertiesFactory.Create(project, data);

            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var debugFrameworkSvcs = new ActiveDebugFrameworkServices(null!, commonServices);
            var result = await debugFrameworkSvcs.GetActiveDebuggingFrameworkPropertyAsync();

            Assert.Equal(framework, result);
        }

        [Fact]
        public async Task SetActiveDebuggingFrameworkPropertyAsync_SetsValue()
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ProjectDebugger.SchemaName, ProjectDebugger.ActiveDebugFrameworkProperty, "FrameworkOne");

            var projectProperties = ProjectPropertiesFactory.Create(project, data);

            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var debugFrameworkSvcs = new ActiveDebugFrameworkServices(null!, commonServices);
            await debugFrameworkSvcs.SetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0");
            // Just verifies it doesn't throw. In other words, the function is trying to set the correct property. The way the property mocks
            // are set up there is no easy way to capture the value being set without rewriting how they work.
        }

        [Theory]
        [InlineData("netcoreapp1.0", "netcoreapp1.0")]
        [InlineData("net461", "net461")]
        [InlineData("", "net462")]
        [InlineData("someframework", "net462")]
        public async Task GetConfiguredProjectForActiveFrameworkAsync_ReturnsCorrectProject(string framework, string selectedConfigFramework)
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ProjectDebugger.SchemaName, ProjectDebugger.ActiveDebugFrameworkProperty, framework);
            var data2 = new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworksProperty, "net462;net461;netcoreapp1.0");

            var projects = ImmutableStringDictionary<ConfiguredProject>.EmptyOrdinal
                            .Add("net461", ConfiguredProjectFactory.Create(null, new StandardProjectConfiguration("Debug|AnyCPU|net461", Empty.PropertiesMap
                                                                                    .Add("Configuration", "Debug")
                                                                                    .Add("Platform", "AnyCPU")
                                                                                    .Add("TargetFramework", "net461"))))
                            .Add("netcoreapp1.0", ConfiguredProjectFactory.Create(null, new StandardProjectConfiguration("Debug|AnyCPU|netcoreapp1.0", Empty.PropertiesMap
                                                                                    .Add("Configuration", "Debug")
                                                                                    .Add("Platform", "AnyCPU")
                                                                                    .Add("TargetFramework", "netcoreapp1.0"))))
                            .Add("net462", ConfiguredProjectFactory.Create(null, new StandardProjectConfiguration("Debug|AnyCPU|net462", Empty.PropertiesMap
                                                                                    .Add("Configuration", "Debug")
                                                                                    .Add("Platform", "AnyCPU")
                                                                                    .Add("TargetFramework", "net462"))));

            var projectProperties = ProjectPropertiesFactory.Create(project, data, data2);
            var projectConfigProvider = new IActiveConfiguredProjectsProviderFactory(MockBehavior.Strict)
                                       .ImplementGetActiveConfiguredProjectsMapAsync(projects);

            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var debugFrameworkSvcs = new ActiveDebugFrameworkServices(projectConfigProvider.Object, commonServices);
            var activeConfiguredProject = await debugFrameworkSvcs.GetConfiguredProjectForActiveFrameworkAsync();
            Assert.NotNull(activeConfiguredProject);
            Assert.Equal(selectedConfigFramework, activeConfiguredProject.ProjectConfiguration.Dimensions.GetValueOrDefault("TargetFramework"));
        }
    }
}
