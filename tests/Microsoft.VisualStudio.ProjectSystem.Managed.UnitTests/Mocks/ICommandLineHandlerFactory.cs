// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class ICommandLineHandlerFactory
    {
        public static ICommandLineHandler ImplementHandle(Action<IWorkspaceProjectContext, IComparable, BuildOptions, BuildOptions, ContextState, IProjectDiagnosticOutputService> action)
        {
            var mock = new Mock<ICommandLineHandler>();

            mock.Setup(h => h.Handle(It.IsAny<IWorkspaceProjectContext>(), It.IsAny<IComparable>(), It.IsAny<BuildOptions>(), It.IsAny<BuildOptions>(), It.IsAny<ContextState>(), It.IsAny<IProjectDiagnosticOutputService>()))
                .Callback(action);

            return mock.Object;
        }
    }
}
