// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectServicesFactory
    {
        public static IUnconfiguredProjectServices Create(IProjectAsynchronousTasksService? asyncTaskService = null, IActiveConfiguredProjectProvider? activeConfiguredProjectProvider = null, IProjectConfigurationsService? projectConfigurationsService = null, IProjectService? projectService = null)
        {
            var mock = new Mock<IUnconfiguredProjectServices>();

            if (asyncTaskService is not null)
            {
                mock.Setup(s => s.ProjectAsynchronousTasks)
                    .Returns(asyncTaskService);
            }

            if (activeConfiguredProjectProvider is not null)
            {
                mock.Setup(s => s.ActiveConfiguredProjectProvider)
                    .Returns(activeConfiguredProjectProvider);
            }

            if (projectConfigurationsService is not null)
            {
                mock.Setup(s => s.ProjectConfigurationsService)
                    .Returns(projectConfigurationsService);
            }

            if (projectService is not null)
            {
                mock.Setup(s => s.ProjectService)
                    .Returns(projectService);
            }

            return mock.Object;
        }
    }
}
