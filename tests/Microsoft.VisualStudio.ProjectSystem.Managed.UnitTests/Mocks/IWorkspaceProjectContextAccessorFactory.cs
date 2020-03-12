// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IWorkspaceProjectContextAccessorFactory
    {
        public static IWorkspaceProjectContextAccessor ImplementContextId(string contextId)
        {
            var mock = new Mock<IWorkspaceProjectContextAccessor>();

            mock.Setup(c => c.ContextId)
                .Returns(contextId);

            return mock.Object;
        }

        public static IWorkspaceProjectContextAccessor ImplementContext(IWorkspaceProjectContext context, string? contextId = null)
        {
            var mock = new Mock<IWorkspaceProjectContextAccessor>();

            mock.Setup(c => c.Context)
                .Returns(context);

            mock.Setup(c => c.ContextId)
                .Returns(contextId!);

            return mock.Object;
        }

        public static IWorkspaceProjectContextAccessor ImplementHostSpecificErrorReporter(Func<object> action)
        {
            var mock = new Mock<IWorkspaceProjectContextAccessor>();

            mock.SetupGet(c => c.HostSpecificErrorReporter)
                .Returns(action);

            return mock.Object;
        }

        public static IWorkspaceProjectContextAccessor Create()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();

            return ImplementContext(context);
        }
    }
}
