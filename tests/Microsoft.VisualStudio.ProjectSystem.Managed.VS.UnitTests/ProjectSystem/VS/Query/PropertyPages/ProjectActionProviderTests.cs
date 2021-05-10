// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    public class ProjectActionProviderTests
    {
        private class TestProperty : BaseProperty { }

        [Fact]
        public async Task WhenNoDimensionsAreGiven_ThenThePropertyIsSetInAllConfigurations()
        {
            var project = UnconfiguredProjectFactory.Create(
                fullPath: @"C:\alpha\beta\MyProject.csproj",
                configuredProject: ConfiguredProjectFactory.Create(
                    services: ConfiguredProjectServicesFactory.Create(
                        IPropertyPagesCatalogProviderFactory.Create(new()
                        {
                            {
                                "Project",
                                IPropertyPagesCatalogFactory.Create(new Dictionary<string, ProjectSystem.Properties.IRule>()
                                {
                                    { "MyPage", IRuleFactory.Create(new Rule
                                        {
                                            Name = "MyPage",
                                            Properties = new()
                                            {
                                                new TestProperty
                                                {
                                                    Name = "MyProperty",
                                                    DataSource = new() { HasConfigurationCondition = true }
                                                }
                                            }
                                        })
                                    }
                                })
                            }
                        }))));

            var projectConfigurations = GetConfigurations();

            var affectedConfigs = new List<string>();

            var queryCacheProvider = IPropertyPageQueryCacheProviderFactory.Create(
                IPropertyPageQueryCacheFactory.Create(
                    projectConfigurations,
                    bindToRule: (config, schemaName, context) => IRuleFactory.Create(
                        name: "MyPage",
                        properties: new[]
                        {
                            IPropertyFactory.Create(
                                "MyProperty",
                                dataSource: IDataSourceFactory.Create(hasConfigurationCondition: true),
                                setValue: o => affectedConfigs.Add(config.Name))
                        })));
            var emptyTargetDimensions = Enumerable.Empty<(string dimension, string value)>();

            var coreActionExecutor = new ProjectSetUIPropertyValueActionCore(
                queryCacheProvider,
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
            var project = UnconfiguredProjectFactory.Create(
                fullPath: @"C:\alpha\beta\MyProject.csproj",
                configuredProject: ConfiguredProjectFactory.Create(
                    services: ConfiguredProjectServicesFactory.Create(
                        IPropertyPagesCatalogProviderFactory.Create(new()
                        {
                            {
                                "Project",
                                IPropertyPagesCatalogFactory.Create(new Dictionary<string, ProjectSystem.Properties.IRule>()
                                {
                                    { "MyPage", IRuleFactory.Create(new Rule
                                        {
                                            Name = "MyPage",
                                            Properties = new()
                                            {
                                                new TestProperty
                                                {
                                                    Name = "MyProperty",
                                                    DataSource = new() { HasConfigurationCondition = true }
                                                }
                                            }
                                        })
                                    }
                                })
                            }
                        }))));
            var projectConfigurations = GetConfigurations();

            var affectedConfigs = new List<string>();

            var queryCacheProvider = IPropertyPageQueryCacheProviderFactory.Create(
                IPropertyPageQueryCacheFactory.Create(
                    projectConfigurations,
                    bindToRule: (config, schemaName, context) => IRuleFactory.Create(
                        name: "MyPage",
                        properties: new[]
                        {
                            IPropertyFactory.Create(
                                "MyProperty",
                                dataSource: IDataSourceFactory.Create(hasConfigurationCondition: true),
                                setValue: o => affectedConfigs.Add(config.Name))
                        })));
            var targetDimensions = new List<(string dimension, string value)>
            {
                ("Configuration", "Release"),
                ("Platform", "x86")
            };

            var coreActionExecutor = new ProjectSetUIPropertyValueActionCore(
                queryCacheProvider,
                pageName: "MyPage",
                propertyName: "MyProperty",
                targetDimensions,
                prop => prop.SetValueAsync("new value"));

            await coreActionExecutor.OnBeforeExecutingBatchAsync(new[] { project });
            bool propertyUpdated = await coreActionExecutor.ExecuteAsync(project);
            coreActionExecutor.OnAfterExecutingBatch();

            Assert.True(propertyUpdated);
            Assert.Single(affectedConfigs);
            Assert.Contains("Release|x86", affectedConfigs);
        }

        [Fact]
        public async Task WhenARuleContainsMultipleProperties_ThenOnlyTheSpecifiedPropertyIsSet()
        {
            var project = UnconfiguredProjectFactory.Create(
                fullPath: @"C:\alpha\beta\MyProject.csproj",
                configuredProject: ConfiguredProjectFactory.Create(
                    services: ConfiguredProjectServicesFactory.Create(
                        IPropertyPagesCatalogProviderFactory.Create(new()
                        {
                            {
                                "Project",
                                IPropertyPagesCatalogFactory.Create(new Dictionary<string, ProjectSystem.Properties.IRule>()
                                {
                                    { "MyPage", IRuleFactory.Create(new Rule
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
                                                    Name = "NotTheCorrectProperty",
                                                    DataSource = new() { HasConfigurationCondition = true }
                                                }
                                            }
                                        })
                                    }
                                })
                            }
                        }))));
            var projectConfigurations = GetConfigurations();

            var unrelatedPropertySet = false;

            var queryCacheProvider = IPropertyPageQueryCacheProviderFactory.Create(
                IPropertyPageQueryCacheFactory.Create(
                    projectConfigurations,
                    bindToRule: (config, schemaName, context) => IRuleFactory.Create(
                        name: "MyPage",
                        properties: new[]
                        {
                            IPropertyFactory.Create("MyProperty", setValue: o => { }),
                            IPropertyFactory.Create("NotTheCorrectProperty", setValue: o => unrelatedPropertySet = true)
                        })));
            var targetDimensions = new List<(string dimension, string value)>
            {
                ("Configuration", "Release"),
                ("Platform", "x86")
            };

            var coreActionExecutor = new ProjectSetUIPropertyValueActionCore(
                queryCacheProvider,
                pageName: "MyPage",
                propertyName: "MyProperty",
                targetDimensions,
                prop => prop.SetValueAsync("new value"));

            await coreActionExecutor.OnBeforeExecutingBatchAsync(new[] { project });
            bool propertyUpdated = await coreActionExecutor.ExecuteAsync(project);
            coreActionExecutor.OnAfterExecutingBatch();

            Assert.True(propertyUpdated);
            Assert.False(unrelatedPropertySet);
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
