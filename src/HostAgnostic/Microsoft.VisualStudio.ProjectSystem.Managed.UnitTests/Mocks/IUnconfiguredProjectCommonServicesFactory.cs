// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectCommonServicesFactory
    {
        public static IUnconfiguredProjectCommonServices Create()
        {
            var mock = new Mock<IUnconfiguredProjectCommonServices>();

            return mock.Object;
        }

        public static IUnconfiguredProjectCommonServices Create(UnconfiguredProject project = null, IPhysicalProjectTree projectTree = null, IProjectThreadingService threadingService = null, 
                                                                ConfiguredProject configuredProject = null, ProjectProperties projectProperties = null,
                                                                IProjectLockService projectLockService = null)
        {
            var mock = new Mock<IUnconfiguredProjectCommonServices>();

            if (project != null)
                mock.Setup(s => s.Project)
                    .Returns(project);

            if (projectTree != null)
                mock.Setup(s => s.ProjectTree)
                    .Returns(projectTree);

            if (threadingService != null)
                mock.Setup(s => s.ThreadingService)
                    .Returns(threadingService);

            if (configuredProject != null)
                mock.Setup(s => s.ActiveConfiguredProject)
                    .Returns(configuredProject);

            if (projectProperties != null)
                mock.Setup(s => s.ActiveConfiguredProjectProperties)
                    .Returns(projectProperties);

            if (projectLockService != null)
                mock.Setup(s => s.ProjectLockService)
                    .Returns(projectLockService);

            return mock.Object;
        }

        public static IUnconfiguredProjectCommonServices ImplementProject(UnconfiguredProject project)
        {
            return Create(project: project);
        }
    }
}
