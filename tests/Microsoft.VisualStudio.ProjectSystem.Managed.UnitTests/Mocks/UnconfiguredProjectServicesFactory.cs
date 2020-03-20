// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class UnconfiguredProjectServicesFactory
    {
        public static UnconfiguredProjectServices Create(IProjectThreadingService threadingService)
        {
            var projectLockService = IProjectLockServiceFactory.Create();

            var mock = new Mock<UnconfiguredProjectServices>();
            mock.SetupGet(p => p.ProjectService)
                .Returns(IProjectServiceFactory.Create(ProjectServicesFactory.Create(threadingService, projectLockService: projectLockService)));

            mock.Setup(p => p.ProjectLockService)
                .Returns(projectLockService);

            return mock.Object;
        }
    }
}
