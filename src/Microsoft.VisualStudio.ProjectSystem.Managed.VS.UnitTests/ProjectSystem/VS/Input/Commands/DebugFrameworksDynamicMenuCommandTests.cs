// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Debug;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [Trait("UnitTest", "ProjectSystem")]
    public class DebugFrameworksDynamicMenuCommandTests
    {
        [Theory]
        [InlineData(-1, false)]
        [InlineData(2, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        public void ExecCommand_VerifyCorrectFrameworkSet(int cmdIndex, bool expected)
        {
            var frameworks = new List<string>(){"net461", "netcoreapp1.0"};
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesFactory()
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(null)
                                               .ImplementGetProjectFrameworksAsync(frameworks);
            if(expected)
            {
                activeDebugFrameworkSvcs.ImplementSetActiveDebuggingFrameworkPropertyAsync(frameworks[cmdIndex]);
            }
            var startupHelper = new Mock<IStartupProjectHelper>();
                                startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                                             .Returns(activeDebugFrameworkSvcs.Object);

            var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
            Assert.Equal(expected, command.ExecCommand(cmdIndex, EventArgs.Empty));

            startupHelper.Verify();
            activeDebugFrameworkSvcs.Verify();
        }

        [Fact]
        public void ExecCommand_HandleNullProject()
        {
            var startupHelper = new Mock<IStartupProjectHelper>();
                                startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                                             .Returns((IActiveDebugFrameworkServices)null);

            var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
            Assert.False(command.ExecCommand(0, EventArgs.Empty));
            startupHelper.Verify();
        }

        [Fact]
        public void QueryStatus_HandleNullProject()
        {
            var startupHelper = new Mock<IStartupProjectHelper>();
                                startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                                             .Returns((IActiveDebugFrameworkServices)null);

            var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
            Assert.False(command.QueryStatusCommand(0, EventArgs.Empty));
            startupHelper.Verify();
        }

        [Fact]
        public void QueryStatus_NullFrameworks()
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesFactory()
                                               .ImplementGetProjectFrameworksAsync(null);
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(activeDebugFrameworkSvcs.Object);

            var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
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
        public void QueryStatus_LessThan2Frameworks(bool createList)
        {
            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesFactory()
                                               .ImplementGetProjectFrameworksAsync(createList? new List<string>(){"netcoreapp1.0"} : null);
            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(activeDebugFrameworkSvcs.Object);

            var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
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
        public void QueryStatus_TestValidFrameworkIndexes(int cmdIndex, string activeFramework)
        {
            var frameworks = new List<string>(){"netcoreapp1.0", "net461", "net462"};

            var activeDebugFrameworkSvcs = new IActiveDebugFrameworkServicesFactory()
                                               .ImplementGetProjectFrameworksAsync(frameworks)
                                               .ImplementGetActiveDebuggingFrameworkPropertyAsync(activeFramework);

            var startupHelper = new Mock<IStartupProjectHelper>();
            startupHelper.Setup(x => x.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles))
                         .Returns(activeDebugFrameworkSvcs.Object);

            var command = new TestDebugFrameworksDynamicMenuCommand(startupHelper.Object);
            Assert.True(command.QueryStatusCommand(cmdIndex, EventArgs.Empty));
            Assert.True(command.Visible);
            Assert.Equal(frameworks[cmdIndex], command.Text);
            Assert.Equal(frameworks[cmdIndex] == activeFramework, command.Checked);
            Assert.True(command.Enabled);

            startupHelper.Verify();
            activeDebugFrameworkSvcs.Verify();
        }

        private class TestDebugFrameworksDynamicMenuCommand : DebugFrameworksDynamicMenuCommand
        {
            public TestDebugFrameworksDynamicMenuCommand(IStartupProjectHelper startupHelper)
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
