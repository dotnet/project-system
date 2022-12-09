// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IWorkspaceMockFactory
    {
        public static IWorkspace ImplementContextId(string contextId)
        {
            var mock = new Mock<IWorkspace>();

            mock.Setup(c => c.ContextId)
                .Returns(contextId);

            return mock.Object;
        }

        public static IWorkspace ImplementContext(IWorkspaceProjectContext context, string? contextId = null)
        {
            var mock = new Mock<IWorkspace>();

            mock.Setup(c => c.Context)
                .Returns(context);

            mock.Setup(c => c.ContextId)
                .Returns(contextId!);

            return mock.Object;
        }

        public static IWorkspace ImplementHostSpecificErrorReporter(Func<object> action)
        {
            var mock = new Mock<IWorkspace>();

            mock.SetupGet(c => c.HostSpecificErrorReporter)
                .Returns(action);

            return mock.Object;
        }

        public static IWorkspace Create()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();

            return ImplementContext(context);
        }
    }
}
