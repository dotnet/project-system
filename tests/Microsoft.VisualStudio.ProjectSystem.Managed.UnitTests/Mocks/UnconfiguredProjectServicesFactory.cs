// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class UnconfiguredProjectServicesFactory
    {
        public static UnconfiguredProjectServices Create(IProjectThreadingService? threadingService = null, IProjectFaultHandlerService? projectFaultHandlerService = null)
        {
            projectFaultHandlerService ??= IProjectFaultHandlerServiceFactory.Create();
            threadingService ??= IProjectThreadingServiceFactory.Create();

            var projectLockService = IProjectLockServiceFactory.Create();

            var mock = new Mock<UnconfiguredProjectServices>();
            mock.SetupGet(p => p.ProjectService)
                .Returns(IProjectServiceFactory.Create(ProjectServicesFactory.Create(threadingService, projectLockService: projectLockService)));

            mock.Setup(p => p.ProjectLockService)
                .Returns(projectLockService);

            mock.Setup(p => p.FaultHandler)
                .Returns(projectFaultHandlerService);

            return mock.Object;
        }
    }
}
