// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    internal static class IWorkspaceProjectContextFactoryFactory
    {
        public static IWorkspaceProjectContextFactory Create()
        {
            return Mock.Of<IWorkspaceProjectContextFactory>();
        }

        public static IWorkspaceProjectContextFactory ImplementCreateProjectContextAsync(Func<string, string, string, Guid, object, string, CancellationToken, Task<IWorkspaceProjectContext>> action)
        {
            var mock = new Mock<IWorkspaceProjectContextFactory>();

            mock.Setup(c => c.CreateProjectContextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(action);

            return mock.Object;
        }
    }
}
