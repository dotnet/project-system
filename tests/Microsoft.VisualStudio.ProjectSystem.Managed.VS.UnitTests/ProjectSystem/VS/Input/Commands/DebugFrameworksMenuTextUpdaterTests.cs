// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public class DebugFrameworkMenuTextUpdaterTests
    {
        [Fact]
        public void Exec_DoesNothing()
        {
            var startupHelper = new Mock<IStartupProjectHelper>();
            var command = new DebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            DebugFrameworkPropertyMenuTextUpdater.ExecHandler(command, EventArgs.Empty);
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_NoStartupProjects()
        {
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray<IActiveDebugFrameworkServices>.Empty);

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_SingleStartupProject_NullFrameworks()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(null);
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_SingleStartupProject_FrameworksLessThan2()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net45" });
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_SingleStartupProject_FrameworkNoActive()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal(string.Format(VSResources.DebugFrameworkMenuText, "net461"), command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_SingleStartupProject_FrameworkNoMatchingActive()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync("net45")
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal(string.Format(VSResources.DebugFrameworkMenuText, "net461"), command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_SingleStartupProject_FrameworkValidActive()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal(string.Format(VSResources.DebugFrameworkMenuText, "netcoreapp1.0"), command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_MultipleStartupProjects_NullFrameworks()
        {
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(null);

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                             .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                             .ImplementGetProjectFrameworksAsync(null);

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_MultipleStartupProjects_FrameworksLessThan2()
        {
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net45" });

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net45" });

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_MultipleStartupProjects_FrameworksOrderDifferent()
        {
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                             .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                             .ImplementGetProjectFrameworksAsync(new List<string>() { "netcoreapp1.0", "net461" });

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_MultipleStartupProjects_DifferentFrameworks()
        {
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                             .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                             .ImplementGetProjectFrameworksAsync(new List<string>() { "net45", "netcoreapp1.0" });

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_MultipleStartupProjects_OneSingleFramework()
        {
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                      .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                      .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                             .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                             .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });

            var activeDebugFrameworkSvcs3 = new IActiveDebugFrameworkServicesMock()
                                      .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                      .ImplementGetProjectFrameworksAsync(new List<string>() { "netcoreapp1.0" });

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object, activeDebugFrameworkSvcs3.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_MultipleStartupProjects_FrameworkNoActive()
        {
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal(string.Format(VSResources.DebugFrameworkMenuText, "net461"), command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_MultipleStartupProjects_FrameworkNoMatchingActive()
        {
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync("net45")
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync("net45")
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal(string.Format(VSResources.DebugFrameworkMenuText, "net461"), command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        [Fact]
        public void QueryStatus_MultipleStartupProjects_FrameworkValidActive()
        {
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                               .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                             .ImplementGetActiveDebuggingFrameworkPropertyAsync("netcoreapp1.0")
                                             .ImplementGetProjectFrameworksAsync(new List<string>() { "net461", "netcoreapp1.0" });

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = new TestDebugFrameworkPropertyMenuTextUpdater(startupHelper.Object);
            command.QueryStatus();
            Assert.True(command.Visible);
            Assert.Equal(string.Format(VSResources.DebugFrameworkMenuText, "netcoreapp1.0"), command.Text);
            Assert.False(command.Checked);
            Assert.True(command.Enabled);
        }

        private class TestDebugFrameworkPropertyMenuTextUpdater : DebugFrameworkPropertyMenuTextUpdater
        {
            public TestDebugFrameworkPropertyMenuTextUpdater(IStartupProjectHelper startupHelper)
                : base(startupHelper)
            {
            }

            protected override void ExecuteSynchronously(Func<Task> asyncFunction)
            {
                asyncFunction.Invoke().Wait();
            }
        }
    }
}
