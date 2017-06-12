// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Build;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    [ProjectSystemTrait]
    public class TargetFrameworkProjectConfigurationDimensionProviderTests
    {
        private const string ProjectXmlTFM =
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netcoreapp1.0</TargetFramework>
  </PropertyGroup>
</Project>";

        private const string ProjectXmlTFMs =
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>netcoreapp1.0;net45</TargetFrameworks>
  </PropertyGroup>
</Project>";

        private const string ProjectXmlTFMAndTFMs =
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>netcoreapp1.0;net45</TargetFrameworks>
    <TargetFramework>netcoreapp1.0</TargetFramework>
  </PropertyGroup>
</Project>";

        [Fact]
        public async Task TargetFrameworkProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync_TFM()
        {
            using (var projectFile = new MsBuildProjectFile(ProjectXmlTFM))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new TargetFrameworkProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);
                Assert.Equal(0, values.Count());
            }
        }

        [Theory]
        [InlineData(ProjectXmlTFMs)]
        [InlineData(ProjectXmlTFMAndTFMs)]
        public async Task TargetFrameworkProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync_TFMs(string projectXml)
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new TargetFrameworkProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);
                Assert.Equal(1, values.Count());
                var value = values.First();
                Assert.Equal(ConfigurationGeneral.TargetFrameworkProperty, value.Key);
                Assert.Equal("netcoreapp1.0", value.Value);
            }
        }

        [Fact]
        public async Task TargetFrameworkProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync_TFM()
        {
            using (var projectFile = new MsBuildProjectFile(ProjectXmlTFM))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new TargetFrameworkProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);
                Assert.Equal(0, values.Count());
            }
        }

        [Theory]
        [InlineData(ProjectXmlTFMs)]
        [InlineData(ProjectXmlTFMAndTFMs)]
        public async Task TargetFrameworkProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync_TFMs(string projectXml)
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new TargetFrameworkProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);
                Assert.Equal(1, values.Count());
                var value = values.First();
                Assert.Equal(ConfigurationGeneral.TargetFrameworkProperty, value.Key);
                string[] dimensionValues = value.Value.ToArray();
                Assert.Equal(2, dimensionValues.Length);
                Assert.Equal("netcoreapp1.0", dimensionValues[0]);
                Assert.Equal("net45", dimensionValues[1]);
            }
        }

        [Theory]
        [InlineData(ConfigurationDimensionChange.Add, ChangeEventStage.Before)]
        [InlineData(ConfigurationDimensionChange.Add, ChangeEventStage.After)]
        [InlineData(ConfigurationDimensionChange.Rename, ChangeEventStage.Before)]
        [InlineData(ConfigurationDimensionChange.Rename, ChangeEventStage.After)]
        [InlineData(ConfigurationDimensionChange.Delete, ChangeEventStage.Before)]
        [InlineData(ConfigurationDimensionChange.Delete, ChangeEventStage.After)]
        public async Task TargetFrameworkProjectConfigurationDimensionProvider_OnDimensionValueChanged(ConfigurationDimensionChange change, ChangeEventStage stage)
        {
            // No changes should happen for TFM so verify that the property is the same before and after
            using (var projectFile = new MsBuildProjectFile(ProjectXmlTFMs))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new TargetFrameworkProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var property = BuildUtilities.GetProperty(projectFile.Project, ConfigurationGeneral.TargetFrameworksProperty);
                string expectedTFMs = property.Value;

                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    change,
                    stage,
                    ConfigurationGeneral.TargetFrameworkProperty,
                    "NewTFM");
                await provider.OnDimensionValueChangedAsync(args);

                Assert.NotNull(property);
                Assert.Equal(expectedTFMs, property.Value);
            }
        }
    }
}
