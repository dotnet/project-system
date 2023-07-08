// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class ProjectActionProviderTests
    {
        private class TestProperty : BaseProperty { }

        // Creates a ConfiguredProject with a given ProjectConfiguration, and a method to
        // call when a property is set in that configuration.
        private static ConfiguredProject CreateConfiguredProject(ProjectConfiguration configuration, Action<string, object?> setValueCallback)
        {
            return ConfiguredProjectFactory.Create(
                projectConfiguration: configuration,
                services: ConfiguredProjectServicesFactory.Create(
                    IPropertyPagesCatalogProviderFactory.Create(new()
                    {
                        {
                            "Project",
                            IPropertyPagesCatalogFactory.Create(new Dictionary<string, ProjectSystem.Properties.IRule>()
                            {
                                    { "MyPage", IRuleFactory.CreateFromRule(new Rule
                                        {
                                            Name = "MyPage",
                                            Properties = new()
                                            {
                                                new TestProperty
                                                {
                                                    Name = "MyProperty",
                                                    DataSource = new() { HasConfigurationCondition = true }
                                                },
                                                new TestProperty
                                                {
                                                    Name = "MyOtherProperty",
                                                    DataSource = new() { HasConfigurationCondition = true }
                                                }
                                            }
                                        },
                                        properties: new[]
                                        {
                                            IPropertyFactory.Create(
                                                "MyProperty",
                                                dataSource: IDataSourceFactory.Create(hasConfigurationCondition: true),
                                                setValue: v => setValueCallback("MyProperty", v)),
                                            IPropertyFactory.Create(
                                                "MyOtherProperty",
                                                dataSource: IDataSourceFactory.Create(hasConfigurationCondition: true),
                                                setValue: v => setValueCallback("MyOtherProperty", v))
                                        })
                                    }
                            })
                        }
                    })));
        }

        private static UnconfiguredProject CreateUnconfiguredProject(ImmutableHashSet<ProjectConfiguration> projectConfigurations, IEnumerable<ConfiguredProject> configuredProjects)
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var project = UnconfiguredProjectFactory.Create(
                fullPath: @"C:\alpha\beta\MyProject.csproj",
                configuredProjects: configuredProjects,
                unconfiguredProjectServices: UnconfiguredProjectServicesFactory.Create(
                    threadingService: threadingService,
                    projectService: IProjectServiceFactory.Create(
                        services: ProjectServicesFactory.Create(
                            threadingService: threadingService)),
                    projectConfigurationsService: IProjectConfigurationsServiceFactory.ImplementGetKnownProjectConfigurationsAsync(projectConfigurations)));
            return project;
        }

        [Fact]
        public async Task WhenNoDimensionsAreGiven_ThenThePropertyIsSetInAllConfigurations()
        {
            var affectedConfigs = new List<string>();
            var projectConfigurations = GetConfigurations();
            var configuredProjects = projectConfigurations.Select(config => CreateConfiguredProject(config, (p, v) => affectedConfigs.Add(config.Name)));
            var project = CreateUnconfiguredProject(projectConfigurations, configuredProjects);

            var emptyTargetDimensions = Enumerable.Empty<(string dimension, string value)>();

            var coreActionExecutor = new ProjectSetUIPropertyValueActionCore(
                pageName: "MyPage",
                propertyName: "MyProperty",
                emptyTargetDimensions,
                prop => prop.SetValueAsync("new value"));

            await coreActionExecutor.OnBeforeExecutingBatchAsync(new[] { project });
            bool propertyUpdated = await coreActionExecutor.ExecuteAsync(project);
            coreActionExecutor.OnAfterExecutingBatch();

            Assert.True(propertyUpdated);
            Assert.Equal(expected: 4, actual: affectedConfigs.Count);
            foreach (var configuration in projectConfigurations)
            {
                Assert.Contains(configuration.Name, affectedConfigs);
            }
        }

        [Fact]
        public async Task WhenDimensionsAreGiven_ThenThePropertyIsOnlySetInTheMatchingConfigurations()
        {
            var affectedConfigs = new List<string>();
            var projectConfigurations = GetConfigurations();
            var configuredProjects = projectConfigurations.Select(config => CreateConfiguredProject(config, (p, v) => affectedConfigs.Add(config.Name)));
            var unconfiguredProject = CreateUnconfiguredProject(projectConfigurations, configuredProjects);

            var targetDimensions = new List<(string dimension, string value)>
            {
                ("Configuration", "Release"),
                ("Platform", "x86")
            };

            var coreActionExecutor = new ProjectSetUIPropertyValueActionCore(
                pageName: "MyPage",
                propertyName: "MyProperty",
                targetDimensions,
                prop => prop.SetValueAsync("new value"));

            await coreActionExecutor.OnBeforeExecutingBatchAsync(new[] { unconfiguredProject });
            bool propertyUpdated = await coreActionExecutor.ExecuteAsync(unconfiguredProject);
            coreActionExecutor.OnAfterExecutingBatch();

            Assert.True(propertyUpdated);
            Assert.Single(affectedConfigs);
            Assert.Contains("Release|x86", affectedConfigs);
        }

        

        [Fact]
        public async Task WhenARuleContainsMultipleProperties_ThenOnlyTheSpecifiedPropertyIsSet()
        {
            var affectedProperties = new List<string>();
            var projectConfigurations = GetConfigurations();
            var configuredProjects = projectConfigurations.Select(config => CreateConfiguredProject(config, (p, v) => affectedProperties.Add(p)));
            var project = CreateUnconfiguredProject(projectConfigurations, configuredProjects);

            var targetDimensions = new List<(string dimension, string value)>
            {
                ("Configuration", "Release"),
                ("Platform", "x86")
            };

            var coreActionExecutor = new ProjectSetUIPropertyValueActionCore(
                pageName: "MyPage",
                propertyName: "MyProperty",
                targetDimensions,
                prop => prop.SetValueAsync("new value"));

            await coreActionExecutor.OnBeforeExecutingBatchAsync(new[] { project });
            bool propertyUpdated = await coreActionExecutor.ExecuteAsync(project);
            coreActionExecutor.OnAfterExecutingBatch();

            Assert.True(propertyUpdated);
            Assert.Single(affectedProperties);
            Assert.Contains("MyProperty", affectedProperties);
        }

        /// <summary>
        /// Returns a set of <see cref="ProjectConfiguration"/>s for testing purposes.
        /// </summary>
        private static ImmutableHashSet<ProjectConfiguration> GetConfigurations()
        {
            return ImmutableHashSet<ProjectConfiguration>.Empty
                .Add(ProjectConfigurationFactory.Create("Debug|x86"))
                .Add(ProjectConfigurationFactory.Create("Debug|x64"))
                .Add(ProjectConfigurationFactory.Create("Release|x86"))
                .Add(ProjectConfigurationFactory.Create("Release|x64"));
        }
    }
}
