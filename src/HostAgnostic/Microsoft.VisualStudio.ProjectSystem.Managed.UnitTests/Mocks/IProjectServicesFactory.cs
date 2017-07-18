// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class IProjectServicesFactory
    {
        public static IProjectServices Create(IProjectThreadingService threadingService = null, IProjectFaultHandlerService faultHandlerService = null, IProjectService projectService = null)
        {
            threadingService = threadingService ?? IProjectThreadingServiceFactory.Create();
            faultHandlerService = faultHandlerService ?? IProjectFaultHandlerServiceFactory.Create();

            var projectServices = new Mock<IProjectServices>();
            projectServices.Setup(u => u.ThreadingPolicy)
                               .Returns(threadingService);

            projectServices.Setup(u => u.FaultHandler)
                .Returns(faultHandlerService);

            projectServices.Setup(u => u.ProjectService)
                .Returns(projectService);

            return projectServices.Object;
        }
    }
}