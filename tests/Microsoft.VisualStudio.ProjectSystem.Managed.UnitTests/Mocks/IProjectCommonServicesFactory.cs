// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectCommonServicesFactory
    {
        public static IProjectCommonServices CreateWithDefaultThreadingPolicy()
        {
            return Create(null);
        }

        public static IProjectCommonServices Create(IProjectThreadingService? threadingService = null, IProjectLockService? projectLockService = null)
        {
            threadingService ??= IProjectThreadingServiceFactory.Create();

            var services = ProjectServicesFactory.Create(threadingService: threadingService, projectLockService: projectLockService);
            var projectService = IProjectServiceFactory.Create(services);

            var mock = new Mock<IProjectCommonServices>();

            mock.SetupGet(s => s.ProjectService)
                .Returns(projectService);

            return mock.Object;
        }
    }
}
