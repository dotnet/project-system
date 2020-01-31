// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;
using Xunit;

namespace Microsoft.VisualStudio.SolutionExplorer
{
    public class ProjectNodeExtenderCommandHandlerTests
    {
        [Fact]
        public void WhenCommandSetIsWrong_QueryStatusReturnsFalse()
        {
            Guid commandSet = Guid.Parse("{4D4BC677-DF3C-490A-97AB-47F92F19E83B}");
            uint commandId = ManagedProjectSystemClientProjectCommandIds.EditProjectFile;

            var commandHandler = CreateCommandHandler();
            uint commandFlags = 0;
            var result = RunQueryStatus(commandSet, commandId, commandHandler, ref commandFlags);

            Assert.False(result);
        }

        [Fact]
        public void WhenCommandIdIsWrong_QueryStatusReturnsFalse()
        {
            Guid commandSet = CommandGroup.ManagedProjectSystemClientProjectCommandSetGuid;
            uint commandId = 0xFFFF;

            var commandHandler = CreateCommandHandler();
            uint commandFlags = 0;
            var result = RunQueryStatus(commandSet, commandId, commandHandler, ref commandFlags);

            Assert.False(result);
        }

        [Fact]
        public void WhenCommandIsEditProjectFile_QueryStatusReturnsTrue()
        {
            Guid commandSet = CommandGroup.ManagedProjectSystemClientProjectCommandSetGuid;
            uint commandId = ManagedProjectSystemClientProjectCommandIds.EditProjectFile;

            var commandHandler = CreateCommandHandler();
            uint commandFlags = 0;
            var result = RunQueryStatus(commandSet, commandId, commandHandler, ref commandFlags);

            Assert.True(result);
            Assert.Equal(expected: (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED), actual: commandFlags);
        }

        [Fact]
        public void WhenCommandSetIsWrong_ExecReturnsNotSupported()
        {
            Guid commandSet = Guid.Parse("{4D4BC677-DF3C-490A-97AB-47F92F19E83B}");
            uint commandId = ManagedProjectSystemClientProjectCommandIds.EditProjectFile;

            var commandHandler = CreateCommandHandler();
            var result = RunExec(commandSet, commandId, commandHandler);

            Assert.Equal(expected: (int)HResult.Ole.Cmd.NotSupported, actual: result);
        }

        [Fact]
        public void WhenCommandIdIsWrong_ExecReturnsNotSupported()
        {
            Guid commandSet = CommandGroup.ManagedProjectSystemClientProjectCommandSetGuid;
            uint commandId = 0xFFFF;

            var commandHandler = CreateCommandHandler();

            var selectedNodes = new List<WorkspaceVisualNodeBase>();
            var result = commandHandler.Exec(selection: selectedNodes, commandSet, commandId, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero);

            Assert.Equal(expected: (int)HResult.Ole.Cmd.NotSupported, actual: result);
        }

        [Fact]
        public void WhenCommandIsEditProjectFile_ExecReturnsOK()
        {
            Guid commandSet = CommandGroup.ManagedProjectSystemClientProjectCommandSetGuid;
            uint commandId = ManagedProjectSystemClientProjectCommandIds.EditProjectFile;

            var commandHandler = CreateCommandHandler();

            var selectedNodes = new List<WorkspaceVisualNodeBase>();

            var result = commandHandler.Exec(selection: selectedNodes, commandSet, commandId, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero);

            Assert.Equal(expected: (int)HResult.OK, actual: result);
        }

        private static JoinableTaskContext GetJoinableTaskContext()
        {
#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
            return new Threading.JoinableTaskContext();
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext
        }

        private static ProjectNodeExtenderCommandHandler CreateCommandHandler()
        {
            return new ProjectNodeExtenderCommandHandler(
                GetJoinableTaskContext(),
                IServiceProviderFactory.ImplementGetService(t => null));
        }

        private static bool RunQueryStatus(Guid commandSet, uint commandId, ProjectNodeExtenderCommandHandler commandHandler, ref uint commandFlags)
        {
            var selectedNodes = new List<WorkspaceVisualNodeBase>();
            string customTitle = string.Empty;
            var result = commandHandler.QueryStatus(selection: selectedNodes, commandSet, commandId, ref commandFlags, ref customTitle);
            return result;
        }

        private static int RunExec(Guid commandSet, uint commandId, ProjectNodeExtenderCommandHandler commandHandler)
        {
            var selectedNodes = new List<WorkspaceVisualNodeBase>();
            var result = commandHandler.Exec(selection: selectedNodes, commandSet, commandId, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero);
            return result;
        }
    }
}
