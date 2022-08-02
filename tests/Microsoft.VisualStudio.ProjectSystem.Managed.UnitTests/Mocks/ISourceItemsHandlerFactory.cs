// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class ISourceItemsHandlerFactory
    {
        public static ISourceItemsHandler ImplementHandle(Action<IWorkspaceProjectContext, IComparable, IImmutableDictionary<string, IProjectChangeDescription>, ContextState, IProjectDiagnosticOutputService> action)
        {
            var mock = new Mock<ISourceItemsHandler>();

            mock.Setup(h => h.Handle(It.IsAny<IWorkspaceProjectContext>(), It.IsAny<IComparable>(), It.IsAny<IImmutableDictionary<string, IProjectChangeDescription>>(), It.IsAny<ContextState>(), It.IsAny<IProjectDiagnosticOutputService>()))
                .Callback(action);

            return mock.Object;
        }
    }
}
