// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Configuration
{
    [ProjectSystemTrait]
    public class TargetFrameworkProjectConfigurationDimensionProviderTests
    {
        private string projectXmlTFM =
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netcoreapp1.0</TargetFramework>
  </PropertyGroup>
</Project>";

        private string projectXmlTFMs =
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>netcoreapp1.0;net45</TargetFrameworks>
  </PropertyGroup>
</Project>";

        [Fact]
        public async void TargetFrameworkProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync_TFM()
        {
            using (var projectFile = new MsBuildProjectFile(projectXmlTFM))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new TargetFrameworkProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);
                Assert.Equal(0, values.Count());
            }
        }

        [Fact]
        public async void TargetFrameworkProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync_TFMs()
        {
            using (var projectFile = new MsBuildProjectFile(projectXmlTFMs))
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
        public async void TargetFrameworkProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync_TFM()
        {
            using (var projectFile = new MsBuildProjectFile(projectXmlTFM))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new TargetFrameworkProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);
                Assert.Equal(0, values.Count());
            }
        }

        [Fact]
        public async void TargetFrameworkProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync_TFMs()
        {
            using (var projectFile = new MsBuildProjectFile(projectXmlTFMs))
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
        public async void TargetFrameworkProjectConfigurationDimensionProvider_OnDimensionValueChanged(ConfigurationDimensionChange change, ChangeEventStage stage)
        {
            // No changes should happen for TFM so verify that the property is the same before and after
            using (var projectFile = new MsBuildProjectFile(projectXmlTFMs))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new TargetFrameworkProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var property = MsBuildUtilities.GetProperty(projectFile.Project, ConfigurationGeneral.TargetFrameworksProperty);
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
