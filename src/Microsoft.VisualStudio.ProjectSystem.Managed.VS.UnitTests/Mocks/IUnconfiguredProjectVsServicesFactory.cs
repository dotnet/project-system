// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.Shell.Interop;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IUnconfiguredProjectVsServicesFactory
    {
        public static IUnconfiguredProjectVsServices Create()
        {
            return Mock.Of<IUnconfiguredProjectVsServices>();
        }

        public static IUnconfiguredProjectVsServices Implement(Func<IVsHierarchy> hierarchyCreator = null,
                                                               Func<IVsProject4> projectCreator = null,
                                                               Func<IProjectThreadingService> threadingServiceCreator = null,
                                                               Func<ProjectProperties> projectProperties = null,
                                                               Func<UnconfiguredProject> unconfiguredProjectCreator = null)
        {
            var mock = new Mock<IUnconfiguredProjectVsServices>();
            if (hierarchyCreator != null)
            {
                mock.SetupGet(h => h.VsHierarchy)
                    .Returns(hierarchyCreator);
            }

            var threadingService = threadingServiceCreator == null ? IProjectThreadingServiceFactory.Create() : threadingServiceCreator();

            mock.SetupGet(h => h.ThreadingService)
                .Returns(threadingService);

            if (projectCreator != null)
            {
                mock.SetupGet(h => h.VsProject)
                    .Returns(projectCreator());
            }

            if (projectProperties != null)
            {
                mock.SetupGet(h => h.ActiveConfiguredProjectProperties).Returns(projectProperties());
            }

            if (unconfiguredProjectCreator != null)
            {
                mock.SetupGet(h => h.Project).Returns(unconfiguredProjectCreator());
            }

            return mock.Object;
        }
    }
}
