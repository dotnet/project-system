// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectServicesFactory
    {
        public static IUnconfiguredProjectServices Create(IProjectAsynchronousTasksService? asyncTaskService = null, IActiveConfiguredProjectProvider? activeConfiguredProjectProvider = null, IProjectConfigurationsService? projectConfigurationsService = null, IProjectService? projectService = null)
        {
            var mock = new Mock<IUnconfiguredProjectServices>();

            if (asyncTaskService != null)
            {
                mock.Setup(s => s.ProjectAsynchronousTasks)
                    .Returns(asyncTaskService);
            }

            if (activeConfiguredProjectProvider != null)
            {
                mock.Setup(s => s.ActiveConfiguredProjectProvider)
                    .Returns(activeConfiguredProjectProvider);
            }

            if (projectConfigurationsService != null)
            {
                mock.Setup(s => s.ProjectConfigurationsService)
                    .Returns(projectConfigurationsService);
            }

            if (projectService != null)
            {
                mock.Setup(s => s.ProjectService)
                    .Returns(projectService);
            }

            return mock.Object;
        }
    }
}
