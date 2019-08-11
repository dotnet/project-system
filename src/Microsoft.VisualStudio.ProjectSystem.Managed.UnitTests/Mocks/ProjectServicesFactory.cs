// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

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
