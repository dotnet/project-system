// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class IActiveConfiguredProjectsProviderFactory
    {
        private readonly Mock<IActiveConfiguredProjectsProvider> _mock;

        public IActiveConfiguredProjectsProviderFactory(MockBehavior mockBehavior = MockBehavior.Strict)
        {
            _mock = new Mock<IActiveConfiguredProjectsProvider>(mockBehavior);
        }

        public IActiveConfiguredProjectsProvider Object => _mock.Object;

        public void Verify()
        {
            _mock.Verify();
        }

        public IActiveConfiguredProjectsProviderFactory ImplementGetActiveConfiguredProjectsMapAsync(ImmutableDictionary<string, ConfiguredProject> configuredProjects)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _mock.Setup(x => x.GetActiveConfiguredProjectsMapAsync())
#pragma warning restore CS0618 // Type or member is obsolete
                              .ReturnsAsync(configuredProjects);
            return this;
        }

        public IActiveConfiguredProjectsProviderFactory ImplementGetActiveConfiguredProjectsAsync(ActiveConfiguredObjects<ConfiguredProject> configuredProjects)
        {
            _mock.Setup(x => x.GetActiveConfiguredProjectsAsync())
                              .ReturnsAsync(configuredProjects);
            return this;
        }

        public IActiveConfiguredProjectsProviderFactory ImplementGetProjectFrameworksAsync(ActiveConfiguredObjects<ProjectConfiguration> projectConfigurations)
        {
            _mock.Setup(x => x.GetActiveProjectConfigurationsAsync())
                              .ReturnsAsync(projectConfigurations);
            return this;
        }
    }
}
