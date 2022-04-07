// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Managed.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public sealed class StartupProjectSingleTargetGlobalBuildPropertyProviderTests
    {
        [Theory]
        //          projectPath              crossTargeting  implicitlyTriggeredBuild  startupProjects                                  globalOptionEnabled expectTargetFrameworkSet
        [InlineData(@"C:\alpha.csproj",      true,           true,                     new[] { @"C:\alpha.csproj" },                    true,               true)]
        [InlineData(@"C:\alpha.csproj",      false,          true,                     new[] { @"C:\alpha.csproj" },                    true,               false)]
        [InlineData(@"C:\alpha.csproj",      true,           false,                    new[] { @"C:\alpha.csproj" },                    true,               false)]
        [InlineData(@"C:\beta.csproj",       true,           true,                     new[] { @"C:\alpha.csproj" },                    true,               false)]
        [InlineData(@"C:\alpha.csproj",      true,           true,                     new[] { @"C:\alpha.csproj", @"C:\beta.csproj" }, true,               false)]
        [InlineData(@"C:\alpha.csproj",      true,           true,                     new[] { @"C:\alpha.csproj" },                    false,              false)]
        public async Task VerifyExpectedBehaviors(string projectPath, bool crossTargeting, bool implicitlyTriggeredBuild, string[] startupProjects, bool globalOptionEnabled, bool expectTargetFrameworkSet)
        {
            var projectService = IProjectServiceFactory.Create();

            var unconfiguredProject = UnconfiguredProjectFactory.Create(fullPath: projectPath);

            ConfiguredProject? configuredProject;
            if (crossTargeting)
            {
                var dimensions = Empty.PropertiesMap
                    .Add("Configuration", "Debug")
                    .Add("Platform", "AnyCPU")
                    .Add("TargetFramework", "netcoreapp1.0");
                var projectConfiguration = new StandardProjectConfiguration("Debug|AnyCPU|netcoreapp1.0", dimensions);
                configuredProject = ConfiguredProjectFactory.Create(projectConfiguration: projectConfiguration, unconfiguredProject: unconfiguredProject);
            }
            else
            {
                var dimensions = Empty.PropertiesMap
                    .Add("Configuration", "Debug")
                    .Add("Platform", "AnyCPU");
                var projectConfiguration = new StandardProjectConfiguration("Debug|AnyCPU", dimensions);
                configuredProject = ConfiguredProjectFactory.Create(projectConfiguration: projectConfiguration, unconfiguredProject: unconfiguredProject);
            }

            var activeDebugFrameworkServices = IActiveDebugFrameworkServicesFactory.ImplementGetActiveDebuggingFrameworkPropertyAsync("myFramework1.0");

            var implicitlyTriggeredBuildState = IImplicityTriggeredBuildStateFactory.Create(implicitlyTriggeredBuild, startupProjects);

            var projectSystemOptions = IProjectSystemOptionsFactory.ImplementGetPreferSingleTargetBuildsForStartupProjectsAsync(ct => globalOptionEnabled);

            var provider = new StartupProjectSingleTargetGlobalBuildPropertyProvider(
                projectService,
                configuredProject,
                activeDebugFrameworkServices,
                implicitlyTriggeredBuildState,
                projectSystemOptions);

            var globalProperties = await provider.GetGlobalPropertiesAsync(CancellationToken.None);

            if (expectTargetFrameworkSet)
            {
                Assert.Equal(expected: 1, actual: globalProperties.Count);
                Assert.Equal(expected: "myFramework1.0", actual: globalProperties[ConfigurationGeneral.TargetFrameworkProperty]);
            }
            else
            {
                Assert.Empty(globalProperties);
            }
        }
    }
}
