// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ConfiguredProjectFactory
    {
        public static ConfiguredProject Create(IProjectCapabilitiesScope? capabilities = null, ProjectConfiguration? projectConfiguration = null, ConfiguredProjectServices? services = null, UnconfiguredProject? unconfiguredProject = null)
        {
            var mock = new Mock<ConfiguredProject>();
            mock.Setup(c => c.Capabilities).Returns(capabilities!);
            mock.Setup(c => c.ProjectConfiguration).Returns(projectConfiguration!);
            mock.Setup(c => c.Services).Returns(services!);
            mock.SetupGet(c => c.UnconfiguredProject).Returns(unconfiguredProject ?? UnconfiguredProjectFactory.Create());
            return mock.Object;
        }

        public static ConfiguredProject ImplementProjectConfiguration(string configuration)
        {
            return ImplementProjectConfiguration(ProjectConfigurationFactory.Create(configuration));
        }

        public static ConfiguredProject ImplementProjectConfiguration(ProjectConfiguration projectConfiguration)
        {
            return Create(projectConfiguration: projectConfiguration);
        }

        public static ConfiguredProject ImplementUnconfiguredProject(UnconfiguredProject project)
        {
            var mock = new Mock<ConfiguredProject>();
            mock.SetupGet(p => p.UnconfiguredProject)
                .Returns(project);

            return mock.Object;
        }
    }
}
