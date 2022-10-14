// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq.Language.Flow;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    internal static class IWorkspaceProjectContextFactoryFactory
    {
        public static IWorkspaceProjectContextFactory ImplementCreateProjectContext(Func<Guid, string, string, EvaluationData, object?, CancellationToken, IWorkspaceProjectContext> action)
        {
            var mock = new Mock<IWorkspaceProjectContextFactory>(MockBehavior.Strict);

            mock.SetupCreateProjectContext().ReturnsAsync(action);

            return mock.Object;
        }

        public static IWorkspaceProjectContextFactory ImplementCreateProjectContextThrows(Exception exception)
        {
            var mock = new Mock<IWorkspaceProjectContextFactory>(MockBehavior.Strict);

            mock.SetupCreateProjectContext().Throws(exception);

            return mock.Object;
        }

        public static ISetup<IWorkspaceProjectContextFactory, Task<IWorkspaceProjectContext>> SetupCreateProjectContext(this Mock<IWorkspaceProjectContextFactory> mock)
        {
            return mock.Setup(c => c.CreateProjectContextAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EvaluationData>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()));
        }
    }
}
