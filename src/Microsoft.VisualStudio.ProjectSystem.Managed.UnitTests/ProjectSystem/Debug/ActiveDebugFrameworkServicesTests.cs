// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectSystemTrait]
    public class ActiveDebugFrameworkServicesTests
    {
        [Theory]
        [InlineData("netcoreapp1.0;net462", new string[] {"netcoreapp1.0", "net462"})]
        [InlineData("net461;netcoreapp1.0;net45;net462", new string[] {"net461", "netcoreapp1.0", "net45", "net462"})]
        public async Task GetProjectFrameworksAsync_ReturnsFrameworksInCorrectOrder(string frameworks, string[] expectedOrder)
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData() {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.TargetFrameworksProperty,
                Value = frameworks
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var debugFrameworkSvcs = new ActiveDebugFrameworkServices(null, commonServices);
            var result = await debugFrameworkSvcs.GetProjectFrameworksAsync();
            
            Assert.Equal(expectedOrder.Length, result.Count);
            for(int i = 0; i < result.Count; i++)
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
            var data = new PropertyPageData() {
                Category = ProjectDebugger.SchemaName,
                PropertyName = ProjectDebugger.ActiveDebugFrameworkProperty,
                Value = framework
            };
            
            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var debugFrameworkSvcs = new ActiveDebugFrameworkServices(null, commonServices);
            var result = await debugFrameworkSvcs.GetActiveDebuggingFrameworkPropertyAsync();
            
            Assert.Equal(framework, result);
        }

        [Fact]
        public async Task SetActiveDebuggingFrameworkPropertyAsync_SetsValue()
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData() {
                Category = ProjectDebugger.SchemaName,
                PropertyName = ProjectDebugger.ActiveDebugFrameworkProperty,
                Value = "FrameworkOne"
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var debugFrameworkSvcs = new ActiveDebugFrameworkServices(null, commonServices);
            await debugFrameworkSvcs.SetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0");
        }

        [Theory]
        [InlineData("netcoreapp1.0", "netcoreapp1.0")]
        [InlineData("net461", "net461")]
        [InlineData("", "net462")]
        [InlineData("someframwork", "net462")]
        public async Task GetConfiguredProjectForActiveFrameworkAsync_ReturnsCorrectProject(string framework, string selectedConfigFramework)
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData() {
                Category = ProjectDebugger.SchemaName,
                PropertyName = ProjectDebugger.ActiveDebugFrameworkProperty,
                Value = framework
            };
            var data2 = new PropertyPageData() {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.TargetFrameworksProperty,
                Value = "net462;net461;netcoreapp1.0"
            };

            var projects = ImmutableDictionary<string, ConfiguredProject>.Empty
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
            var projectConfgProvider = new IActiveConfiguredProjectsProviderFactory(MockBehavior.Strict)
                                       .ImplementGetActiveConfiguredProjectsMapAsync(projects);
            
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var debugFrameworkSvcs = new ActiveDebugFrameworkServices(projectConfgProvider.Object, commonServices);
            var activeConfiguredProject = await debugFrameworkSvcs.GetConfiguredProjectForActiveFrameworkAsync();
            Assert.Equal(selectedConfigFramework,  activeConfiguredProject.ProjectConfiguration.Dimensions.GetValueOrDefault("TargetFramework"));

        }

        //[Fact]
        //public void ExecCommand_HandleNullProject()
        //{
        //    var startupHelper = new Mock<IStartupProjectHelper>();
        //                        startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
        //                                     .Returns((IActiveDebugFrameworkServices)null);

        //    var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
        //    Assert.Equal(false, command.ExecCommand(0, EventArgs.Empty));
        //    startupHelper.Verify();
        //}

        //[Fact]
        //public void QueryStatus_HandleNullProject()
        //{
        //    var startupHelper = new Mock<IStartupProjectHelper>();
        //                        startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
        //                                     .Returns((IActiveDebugFrameworkServices)null);

        //    var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
        //    Assert.Equal(false, command.QueryStatusCommand(0, EventArgs.Empty));
        //    startupHelper.Verify();
        //}

        //[Fact]
        //public void QueryStatus_NullFrameworks()
        //{
        //    var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesFactory()
        //                                       .ImplementGetProjectFrameworksAsync(null);
        //    var startupHelper = new Mock<IStartupProjectHelper>();
        //    startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
        //                 .Returns(activeDebugFrameworkSvcs.Object);

        //    var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
        //    Assert.True(command.QueryStatusCommand(0, EventArgs.Empty));
        //    Assert.False(command.Visible);
        //    Assert.Equal("", command.Text);
        //    Assert.False(command.Checked);
        //    Assert.False(command.Enabled);

        //    startupHelper.Verify();
        //    activeDebugFrameworkSvcs.Verify();
        //}

        //[Theory]
        //[InlineData(false)]
        //[InlineData(true)]
        //public void QueryStatus_LessThan2Frameworks(bool createList)
        //{
        //    var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesFactory()
        //                                       .ImplementGetProjectFrameworksAsync(createList? new List<string>(){"netcoreapp1.0"} : null);
        //    var startupHelper = new Mock<IStartupProjectHelper>();
        //    startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
        //                 .Returns(activeDebugFrameworkSvcs.Object);

        //    var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
        //    Assert.True(command.QueryStatusCommand(0, EventArgs.Empty));
        //    Assert.False(command.Visible);
        //    Assert.Equal("", command.Text);
        //    Assert.False(command.Checked);
        //    Assert.False(command.Enabled);

        //    startupHelper.Verify();
        //    activeDebugFrameworkSvcs.Verify();
        //}

        //[Theory]
        //[InlineData(0, "netcoreapp1.0")]
        //[InlineData(1, "net461")]
        //[InlineData(2, "net461")]
        //[InlineData(2, "net462")]
        //public void QueryStatus_TestValidFrameworkIndexes(int cmdIndex, string activeFramework)
        //{
        //    var frameworks = new List<string>(){"netcoreapp1.0", "net461", "net462"};

        //    var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesFactory()
        //                                       .ImplementGetProjectFrameworksAsync(frameworks)
        //                                       .ImplementGetActiveDebuggingFrameworkPropertyAsync(activeFramework);

        //    var startupHelper = new Mock<IStartupProjectHelper>();
        //    startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
        //                 .Returns(activeDebugFrameworkSvcs.Object);

        //    var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
        //    Assert.True(command.QueryStatusCommand(cmdIndex, EventArgs.Empty));
        //    Assert.True(command.Visible);
        //    Assert.Equal(frameworks[cmdIndex], command.Text);
        //    Assert.Equal(frameworks[cmdIndex] == activeFramework, command.Checked);
        //    Assert.True(command.Enabled);

        //    startupHelper.Verify();
        //    activeDebugFrameworkSvcs.Verify();
        //}

        //class TestDebugFrameworksDynamicMenuCommand : DebugFrameworksDynamicMenuCommand
        //{
        //    public TestDebugFrameworksDynamicMenuCommand(IStartupProjectHelper startupHelper)
        //        : base(startupHelper)
        //    {
        //    }

        //    protected override void ExecuteSynchronously(Func<Task> asyncFunction)
        //    {
        //        asyncFunction.Invoke().Wait();
        //    }
        //}
    }
}
