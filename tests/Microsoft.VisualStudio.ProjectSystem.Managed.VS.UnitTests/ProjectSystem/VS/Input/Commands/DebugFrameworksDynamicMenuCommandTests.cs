// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Threading;

#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public class DebugFrameworksDynamicMenuCommandTests
    {
        [Fact]
        public void ExecCommand_HandleNoStartupProjects()
        {
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray<IActiveDebugFrameworkServices>.Empty);

            var command = CreateInstance(startupHelper.Object);
            Assert.False(command.ExecCommand(0, EventArgs.Empty));
            startupHelper.Verify();
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(2, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        public void ExecCommand_SingleStartupProject_VerifyCorrectFrameworkSet(int cmdIndex, bool expected)
        {
            var frameworks = new List<string>() { "net461", "netcoreapp1.0" };
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(frameworks);
            if (expected)
            {
                activeDebugFrameworkSvcs.ImplementSetActiveDebuggingFrameworkPropertyAsync(frameworks[cmdIndex]);
            }
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs.Object));

            var command = CreateInstance(startupHelper.Object);
            Assert.Equal(expected, command.ExecCommand(cmdIndex, EventArgs.Empty));

            startupHelper.Verify();
            activeDebugFrameworkSvcs.Verify();
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(2, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        public void ExecCommand_MultipleStartupProjects_VerifyCorrectFrameworkSet(int cmdIndex, bool expected)
        {
            var frameworks1 = new List<string>() { "net461", "netcoreapp1.0" };
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(frameworks1);

            var frameworks2 = new List<string>() { "net461", "netcoreapp1.0" };
            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(frameworks2);
            if (expected)
            {
                activeDebugFrameworkSvcs1.ImplementSetActiveDebuggingFrameworkPropertyAsync(frameworks1[cmdIndex]);
                activeDebugFrameworkSvcs2.ImplementSetActiveDebuggingFrameworkPropertyAsync(frameworks2[cmdIndex]);
            }
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = CreateInstance(startupHelper.Object);
            Assert.Equal(expected, command.ExecCommand(cmdIndex, EventArgs.Empty));

            startupHelper.Verify();
            activeDebugFrameworkSvcs1.Verify();
            activeDebugFrameworkSvcs2.Verify();
        }

        [Fact]
        public void QueryStatus_HandleNoStartupProjects()
        {
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray<IActiveDebugFrameworkServices>.Empty);

            var command = CreateInstance(startupHelper.Object);
            Assert.False(command.QueryStatusCommand(0, EventArgs.Empty));
            startupHelper.Verify();
        }

        [Fact]
        public void QueryStatus_SingleStartupProject_NullFrameworks()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetProjectFrameworksAsync(null);
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs.Object));

            var command = CreateInstance(startupHelper.Object);
            Assert.True(command.QueryStatusCommand(0, EventArgs.Empty));
            Assert.False(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.False(command.Enabled);

            startupHelper.Verify();
            activeDebugFrameworkSvcs.Verify();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void QueryStatus_SingleStartupProject_LessThan2Frameworks(bool createList)
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetProjectFrameworksAsync(createList ? new List<string>() { "netcoreapp1.0" } : null);
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs.Object));

            var command = CreateInstance(startupHelper.Object);
            Assert.True(command.QueryStatusCommand(0, EventArgs.Empty));
            Assert.False(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.False(command.Enabled);

            startupHelper.Verify();
            activeDebugFrameworkSvcs.Verify();
        }

        [Theory]
        [InlineData(0, "netcoreapp1.0")]
        [InlineData(1, "net461")]
        [InlineData(2, "net461")]
        [InlineData(2, "net462")]
        public void QueryStatus_SingleStartupProject_TestValidFrameworkIndexes(int cmdIndex, string activeFramework)
        {
            var frameworks = new List<string>() { "netcoreapp1.0", "net461", "net462" };

            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetProjectFrameworksAsync(frameworks)
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(activeFramework);

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs.Object));

            var command = CreateInstance(startupHelper.Object);
            Assert.True(command.QueryStatusCommand(cmdIndex, EventArgs.Empty));
            Assert.True(command.Visible);
            Assert.Equal(frameworks[cmdIndex], command.Text);
            Assert.Equal(frameworks[cmdIndex] == activeFramework, command.Checked);
            Assert.True(command.Enabled);

            startupHelper.Verify();
            activeDebugFrameworkSvcs.Verify();
        }

        [Fact]
        public void QueryStatus_MultipleStartupProjects_NullFrameworks()
        {
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetProjectFrameworksAsync(null);

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                              .ImplementGetProjectFrameworksAsync(null);

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = CreateInstance(startupHelper.Object);
            Assert.True(command.QueryStatusCommand(0, EventArgs.Empty));
            Assert.False(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.False(command.Enabled);

            startupHelper.Verify();
            activeDebugFrameworkSvcs1.Verify();
            activeDebugFrameworkSvcs2.Verify();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void QueryStatus_MultipleStartupProjects_LessThan2Frameworks(bool createList)
        {
            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetProjectFrameworksAsync(createList ? new List<string>() { "netcoreapp1.0" } : null);

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                              .ImplementGetProjectFrameworksAsync(createList ? new List<string>() { "netcoreapp1.0" } : null);

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = CreateInstance(startupHelper.Object);
            Assert.True(command.QueryStatusCommand(0, EventArgs.Empty));
            Assert.False(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.False(command.Enabled);

            startupHelper.Verify();
            activeDebugFrameworkSvcs1.Verify();
            activeDebugFrameworkSvcs2.Verify();
        }

        [Fact]
        public void QueryStatus_MultipleStartupProjects_OrderDifferent()
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

            var command = CreateInstance(startupHelper.Object);
            Assert.True(command.QueryStatusCommand(0, EventArgs.Empty));
            Assert.False(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.False(command.Enabled);

            startupHelper.Verify();
            activeDebugFrameworkSvcs1.Verify();
            activeDebugFrameworkSvcs2.Verify();
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

            var command = CreateInstance(startupHelper.Object);
            Assert.True(command.QueryStatusCommand(0, EventArgs.Empty));
            Assert.False(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.False(command.Enabled);

            startupHelper.Verify();
            activeDebugFrameworkSvcs1.Verify();
            activeDebugFrameworkSvcs2.Verify();
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

            var command = CreateInstance(startupHelper.Object);
            Assert.True(command.QueryStatusCommand(0, EventArgs.Empty));
            Assert.False(command.Visible);
            Assert.Equal("", command.Text);
            Assert.False(command.Checked);
            Assert.False(command.Enabled);

            startupHelper.Verify();
            activeDebugFrameworkSvcs1.Verify();
            activeDebugFrameworkSvcs2.Verify();
            activeDebugFrameworkSvcs3.Verify();
        }

        [Theory]
        [InlineData(0, "netcoreapp1.0")]
        [InlineData(1, "net461")]
        [InlineData(2, "net461")]
        [InlineData(2, "net462")]
        public void QueryStatus_MultipleStartupProjects_TestValidFrameworkIndexes(int cmdIndex, string activeFramework)
        {
            var frameworks1 = new List<string>() { "netcoreapp1.0", "net461", "net462" };

            var activeDebugFrameworkSvcs1 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetProjectFrameworksAsync(frameworks1)
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(activeFramework);

            var frameworks2 = new List<string>() { "netcoreapp1.0", "net461", "net462" };

            var activeDebugFrameworkSvcs2 = new IActiveDebugFrameworkServicesMock()
                                               .ImplementGetProjectFrameworksAsync(frameworks2)
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(activeFramework);

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(ImmutableArray.Create(activeDebugFrameworkSvcs1.Object, activeDebugFrameworkSvcs2.Object));

            var command = CreateInstance(startupHelper.Object);
            Assert.True(command.QueryStatusCommand(cmdIndex, EventArgs.Empty));
            Assert.True(command.Visible);
            Assert.Equal(frameworks1[cmdIndex], command.Text);
            Assert.Equal(frameworks1[cmdIndex] == activeFramework, command.Checked);
            Assert.Equal(frameworks2[cmdIndex], command.Text);
            Assert.Equal(frameworks2[cmdIndex] == activeFramework, command.Checked);
            Assert.True(command.Enabled);

            startupHelper.Verify();
            activeDebugFrameworkSvcs1.Verify();
            activeDebugFrameworkSvcs2.Verify();
        }

        private static DebugFrameworksDynamicMenuCommand CreateInstance(IStartupProjectHelper startupProjectHelper)
        {
            return new DebugFrameworksDynamicMenuCommand(startupProjectHelper, new JoinableTaskContext());
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext
        }

        private class TestDebugFrameworksDynamicMenuCommand : DebugFrameworksDynamicMenuCommand
        {
            public TestDebugFrameworksDynamicMenuCommand(IStartupProjectHelper startupHelper, JoinableTaskContext joinableTaskContext)
                : base(startupHelper, joinableTaskContext)
            {
            }
        }
    }
}
