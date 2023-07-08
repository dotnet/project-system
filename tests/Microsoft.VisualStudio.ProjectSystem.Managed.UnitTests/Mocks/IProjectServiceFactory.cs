// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectServiceFactory
    {
        public static IProjectService Create(ProjectServices? services = null, IProjectCapabilitiesScope? scope = null, ConfiguredProject? configuredProject = null)
        {
            var mock = new Mock<IProjectService>();

            services ??= ProjectServicesFactory.Create(projectService: mock.Object);

            mock.Setup(p => p.Services)
                   .Returns(services);

            if (scope is not null)
            {
                mock.Setup(p => p.LoadedUnconfiguredProjects)
                    .Returns(new[] { UnconfiguredProjectFactory.Create(scope: scope, configuredProject: configuredProject) });
            }

            return mock.Object;
        }

        public static IProjectService Create(IEnumerable<UnconfiguredProject> loadedUnconfiguredProjects, ProjectServices? services = null, IProjectCapabilitiesScope? scope = null)
        {
            var mock = new Mock<IProjectService>();

            services ??= ProjectServicesFactory.Create(projectService: mock.Object);

            mock.Setup(p => p.Services)
                .Returns(services);

            mock.Setup(p => p.LoadedUnconfiguredProjects)
                .Returns(loadedUnconfiguredProjects);

            return mock.Object;
        }
    }
}
