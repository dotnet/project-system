// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class TargetFrameworkGlobalBuildPropertyProviderTests
    {
        [Fact]
        public async Task VerifyTargetFrameworkOverrideForCrossTargetingBuild()
        {
            var dimensions = Empty.PropertiesMap
                .Add("Configuration", "Debug")
                .Add("Platform", "AnyCPU")
                .Add("TargetFramework", "netcoreapp1.0");
            var projectConfiguration = new StandardProjectConfiguration("Debug|AnyCPU|netcoreapp1.0", dimensions);
            var configuredProject = ConfiguredProjectFactory.Create(projectConfiguration: projectConfiguration);
            var projectService = IProjectServiceFactory.Create();
            var provider = new TargetFrameworkGlobalBuildPropertyProvider(projectService, configuredProject);

            var properties = await provider.GetGlobalPropertiesAsync(CancellationToken.None);
            Assert.Single(properties);
            Assert.Equal("TargetFramework", properties.Keys.First());
            Assert.Equal(string.Empty, properties.Values.First());
        }

        [Fact]
        public async Task VerifyNoTargetFrameworkOverrideForRegularBuild()
        {
            var dimensions = Empty.PropertiesMap
                .Add("Configuration", "Debug")
                .Add("Platform", "AnyCPU");
            var projectConfiguration = new StandardProjectConfiguration("Debug|AnyCPU", dimensions);
            var configuredProject = ConfiguredProjectFactory.Create(projectConfiguration: projectConfiguration);
            var projectService = IProjectServiceFactory.Create();
            var provider = new TargetFrameworkGlobalBuildPropertyProvider(projectService, configuredProject);

            var properties = await provider.GetGlobalPropertiesAsync(CancellationToken.None);
            Assert.Equal(0, properties.Count);
        }
    }
}
