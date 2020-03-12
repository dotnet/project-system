// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;
using Xunit;

namespace Microsoft.VisualStudio.SolutionExplorer
{
    public class ProjectNodeExtenderTests
    {
        [Fact]
        public void WhenNodeIsNull_NoCommandHandlerIsReturned()
        {
            var extender = new ProjectNodeExtender(GetJoinableTaskContext(), IServiceProviderFactory.ImplementGetService(t => null));

            var commandHandler = extender.ProvideCommandHandler(null!);

            Assert.Null(commandHandler);
        }

        [Fact]
        public void WhenSelectionKindIsWrong_NoCommandHandlerIsReturned()
        {
            var extender = new ProjectNodeExtender(GetJoinableTaskContext(), IServiceProviderFactory.ImplementGetService(t => null));
            var node = WorkspaceVisualNodeBaseFactory.Implement(
                selectionKind: Guid.Parse("{95D7E5E9-08FA-40FB-9010-2CCEEC6D54C1}"),
                nodeMoniker: "Test.csproj",
                selectionMoniker: "Test.csproj");

            var commandHandler = extender.ProvideCommandHandler(node);

            Assert.Null(commandHandler);
        }

        [Fact]
        public void WhenExtensionIsWrong_NoCommandHandlerIsReturned()
        {
            var extender = new ProjectNodeExtender(GetJoinableTaskContext(), IServiceProviderFactory.ImplementGetService(t => null));
            var node = WorkspaceVisualNodeBaseFactory.Implement(
                selectionKind: CloudEnvironment.SolutionViewProjectGuid,
                nodeMoniker: "Test.notMyProj",
                selectionMoniker: "Test.notMyProj");

            var commandHandler = extender.ProvideCommandHandler(node);

            Assert.Null(commandHandler);
        }

        [Fact]
        public void WhenNodeRepresentsAManagedProject_ACommandHandlerIsReturned()
        {
            var extender = new ProjectNodeExtender(GetJoinableTaskContext(), IServiceProviderFactory.ImplementGetService(t => null));
            var node = WorkspaceVisualNodeBaseFactory.Implement(
                selectionKind: CloudEnvironment.SolutionViewProjectGuid,
                nodeMoniker: "Test.csproj",
                selectionMoniker: "Test.csproj");

            var commandHandler = extender.ProvideCommandHandler(node);

            Assert.NotNull(commandHandler);
        }

        private JoinableTaskContext GetJoinableTaskContext()
        {
#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
            return new JoinableTaskContext();
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext
        }
    }
}
