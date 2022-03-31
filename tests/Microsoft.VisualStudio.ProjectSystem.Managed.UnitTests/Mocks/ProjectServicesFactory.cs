// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ProjectServicesFactory
    {
        public static ProjectServices Create(IProjectThreadingService? threadingService = null, IProjectFaultHandlerService? faultHandlerService = null, IProjectService? projectService = null, IProjectLockService? projectLockService = null)
        {
            threadingService ??= IProjectThreadingServiceFactory.Create();
            faultHandlerService ??= IProjectFaultHandlerServiceFactory.Create();

            var projectServices = new Mock<ProjectServices>();
            projectServices.Setup(u => u.ThreadingPolicy)
                               .Returns(threadingService);

            projectServices.Setup(u => u.FaultHandler)
                .Returns(faultHandlerService);

            projectServices.Setup<IProjectService?>(u => u.ProjectService)
                .Returns(projectService);

            projectServices.Setup(u => u.ProjectLockService)
                .Returns(projectLockService!);

            return projectServices.Object;
        }
    }
}
