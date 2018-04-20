// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Build;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    [Trait("UnitTest", "ProjectSystem")]
    public class ConfigurationProjectConfigurationDimensionProviderTests
    {
        private const string Configurations = nameof(Configurations);

        private string projectXml =
@"<Project>
  <PropertyGroup>
    <Configurations>Debug;Release;CustomConfiguration</Configurations>
  </PropertyGroup>
</Project>";

        [Fact]
        public async Task GetDefaultValuesForDimensionsAsync()
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);
            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);
            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);

            Assert.Single(values);
            var value = values.First();
            Assert.Equal(ConfigurationGeneral.ConfigurationProperty, value.Key);
            Assert.Equal("Debug", value.Value);
        }

        [Fact]
        public async Task GetDefaultValuesForDimensionsAsync_NoPropertyValue()
        {
            var projectAccessor = IProjectAccessorFactory.Create("<Project />");
            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);
            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);

            Assert.Empty(values);
        }

        [Fact]
        public async Task GetProjectConfigurationDimensionsAsync()
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);
            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);
            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);

            Assert.Single(values);
            var value = values.First();
            Assert.Equal(ConfigurationGeneral.ConfigurationProperty, value.Key);
            string[] dimensionValues = value.Value.ToArray();
            Assert.Equal(3, dimensionValues.Length);
            Assert.Equal("Debug", dimensionValues[0]);
            Assert.Equal("Release", dimensionValues[1]);
            Assert.Equal("CustomConfiguration", dimensionValues[2]);
        }

        [Fact]
        public async Task GetProjectConfigurationDimensionsAsync_NoPropertyValue()
        {
            var projectAccessor = IProjectAccessorFactory.Create("<Project />");

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);
            Assert.Empty(values);
        }

        [Fact]
        public async Task OnDimensionValueChanged_Add()
        {
            var project = ProjectRootElementFactory.Create(projectXml);
            var projectAccessor = IProjectAccessorFactory.Create(project);

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            // On ChangeEventStage.After nothing should be changed
            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Add,
                ChangeEventStage.After,
                ConfigurationGeneral.ConfigurationProperty,
                "CustomConfig");
            await provider.OnDimensionValueChangedAsync(args);
            var property = BuildUtilities.GetProperty(project, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration", property.Value);

            // On ChangeEventStage.Before the property should be added
            args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Add,
                ChangeEventStage.Before,
                ConfigurationGeneral.ConfigurationProperty,
                "CustomConfig");
            await provider.OnDimensionValueChangedAsync(args);
            property = BuildUtilities.GetProperty(project, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration;CustomConfig", property.Value);
        }

        [Fact]
        public async Task OnDimensionValueChanged_Remove()
        {
            var project = ProjectRootElementFactory.Create(projectXml);
            var projectAccessor = IProjectAccessorFactory.Create(project);

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            // On ChangeEventStage.After nothing should be changed
            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Delete,
                ChangeEventStage.After,
                ConfigurationGeneral.ConfigurationProperty,
                "CustomConfiguration");
            await provider.OnDimensionValueChangedAsync(args);
            var property = BuildUtilities.GetProperty(project, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration", property.Value);

            // On ChangeEventStage.Before the property should be removed
            args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Delete,
                ChangeEventStage.Before,
                ConfigurationGeneral.ConfigurationProperty,
                "CustomConfiguration");
            await provider.OnDimensionValueChangedAsync(args);
            property = BuildUtilities.GetProperty(project, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release", property.Value);
        }

        [Fact]
        public async Task OnDimensionValueChanged_Remove_MissingValue()
        {
            var project = ProjectRootElementFactory.Create(projectXml);
            var projectAccessor = IProjectAccessorFactory.Create(project);

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Delete,
                ChangeEventStage.Before,
                ConfigurationGeneral.ConfigurationProperty,
                "NonExistantConfiguration");
            await Assert.ThrowsAsync<ArgumentException>(() => provider.OnDimensionValueChangedAsync(args));
            var property = BuildUtilities.GetProperty(project, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration", property.Value);
        }

        [Fact]
        public async Task OnDimensionValueChanged_Rename()
        {
            var project = ProjectRootElementFactory.Create(projectXml);
            var projectAccessor = IProjectAccessorFactory.Create(project);

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            // On ChangeEventStage.Before nothing should be changed
            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Rename,
                ChangeEventStage.Before,
                ConfigurationGeneral.ConfigurationProperty,
                "RenamedConfiguration",
                "CustomConfiguration");
            await provider.OnDimensionValueChangedAsync(args);
            var property = BuildUtilities.GetProperty(project, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration", property.Value);

            // On ChangeEventStage.Before the property should be renamed
            args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Rename,
                ChangeEventStage.After,
                ConfigurationGeneral.ConfigurationProperty,
                "RenamedConfiguration",
                "CustomConfiguration");
            await provider.OnDimensionValueChangedAsync(args);
            property = BuildUtilities.GetProperty(project, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;RenamedConfiguration", property.Value);
        }

        [Fact]
        public async Task OnDimensionValueChanged_Rename_MissingValue()
        {
            var project = ProjectRootElementFactory.Create(projectXml);
            var projectAccessor = IProjectAccessorFactory.Create(project);

            var provider = new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Rename,
                ChangeEventStage.After,
                ConfigurationGeneral.ConfigurationProperty,
                "RenamedConfiguration",
                "NonExistantConfiguration");
            await Assert.ThrowsAsync<ArgumentException>(() => provider.OnDimensionValueChangedAsync(args));
            var property = BuildUtilities.GetProperty(project, Configurations);
            Assert.NotNull(property);
            Assert.Equal("Debug;Release;CustomConfiguration", property.Value);
        }

        [Theory]
        [InlineData("Debug",               "Debug")]
        [InlineData(" Debug ",             "Debug")]
        [InlineData("Release",             "Release")]
        [InlineData("Debug;Release",       "Debug")]
        [InlineData(";Debug;Release",      "Debug")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_ReturnsFirstValue(string configurations, string expected)
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
    <Configurations>{configurations}</Configurations>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Configuration", result.First().Key);
            Assert.Equal(expected, result.First().Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(";")]
        [InlineData(" ;")]
        [InlineData(" ; ")]
        [InlineData(";;;")]
        [InlineData("$(Property)")]
        [InlineData("Foo_$(Property)")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenConfigurationsIsEmpty_ReturnsEmpty(string configurations)
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
    <Configurations>{configurations}</Configurations>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenConfigurationsIsMissing_ReturnsEmpty()
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Empty(result);
        }

        private static ConfigurationProjectConfigurationDimensionProvider CreateInstance(string projectXml)
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);

            return new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);
        }
    }
}
