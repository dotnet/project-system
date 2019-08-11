// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

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
