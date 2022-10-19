// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IProjectEvaluationHandlerFactory
    {
        public static IProjectEvaluationHandler ImplementHandle(
            Action<IWorkspaceProjectContext, ProjectConfiguration, IComparable, IProjectChangeDescription, ContextState, IManagedProjectDiagnosticOutputService> action,
            string? projectEvaluationRule = null)
        {
            var mock = new Mock<IProjectEvaluationHandler>();

            mock.Setup(
                h => h.Handle(
                    It.IsAny<IWorkspaceProjectContext>(),
                    It.IsAny<ProjectConfiguration>(),
                    It.IsAny<IComparable>(),
                    It.IsAny<IProjectChangeDescription>(),
                    It.IsAny<ContextState>(),
                    It.IsAny<IManagedProjectDiagnosticOutputService>()))
                .Callback(action);

            mock.SetupGet(o => o.ProjectEvaluationRule).Returns(projectEvaluationRule ?? "MyEvaluationRule");

            return mock.Object;
        }
    }
}
