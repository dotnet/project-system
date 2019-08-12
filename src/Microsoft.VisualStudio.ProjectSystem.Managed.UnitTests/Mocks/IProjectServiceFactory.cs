// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

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

            if (scope != null)
            {
                mock.Setup(p => p.LoadedUnconfiguredProjects)
                    .Returns(new[] { UnconfiguredProjectFactory.Create(scope: scope, configuredProject: configuredProject) });
            }

            return mock.Object;
        }
    }
}
