// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Xunit;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Configuration
{
    [ProjectSystemTrait]
    public class ConfigurationProjectConfigurationDimensionProviderTests
    {
        const string Configurations = nameof(Configurations);

        private string projectXml =
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <Configurations>Debug;Release;CustomConfiguration</Configurations>
  </PropertyGroup>
</Project>";

        [Fact]
        public async void ConfigurationProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);
                Assert.Equal(1, values.Count());
                var value = values.First();
                Assert.Equal(ConfigurationGeneral.ConfigurationProperty, value.Key);
                Assert.Equal("Debug", value.Value);
            }
        }

        [Fact]
        public async void ConfigurationProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync_NoPropertyValue()
        {
            using (var projectFile = new MsBuildProjectFile())
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);
                Assert.Equal(0, values.Count());
            }
        }

        [Fact]
        public async void ConfigurationProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);
                Assert.Equal(1, values.Count());
                var value = values.First();
                Assert.Equal(ConfigurationGeneral.ConfigurationProperty, value.Key);
                string[] dimensionValues = value.Value.ToArray();
                Assert.Equal(3, dimensionValues.Length);
                Assert.Equal("Debug", dimensionValues[0]);
                Assert.Equal("Release", dimensionValues[1]);
                Assert.Equal("CustomConfiguration", dimensionValues[2]);
            }
        }

        public async void ConfigurationProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync_NoPropertyValue()
        {
            using (var projectFile = new MsBuildProjectFile())
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);
                Assert.Equal(0, values.Count());
            }
        }

        [Fact]
        public async void ConfigurationProjectConfigurationDimensionProvider_OnDimensionValueChanged_Add()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                // On ChangeEventStage.After nothing should be changed
                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Add,
                    ChangeEventStage.After,
                    ConfigurationGeneral.ConfigurationProperty,
                    "CustomConfig");
                await provider.OnDimensionValueChangedAsync(args);
                var property = MsBuildUtilities.GetProperty(projectFile.Project, Configurations);
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
                property = MsBuildUtilities.GetProperty(projectFile.Project, Configurations);
                Assert.NotNull(property);
                Assert.Equal("Debug;Release;CustomConfiguration;CustomConfig", property.Value);
            }
        }

        [Fact]
        public async void ConfigurationProjectConfigurationDimensionProvider_OnDimensionValueChanged_Remove()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                // On ChangeEventStage.After nothing should be changed
                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Delete,
                    ChangeEventStage.After,
                    ConfigurationGeneral.ConfigurationProperty,
                    "CustomConfiguration");
                await provider.OnDimensionValueChangedAsync(args);
                var property = MsBuildUtilities.GetProperty(projectFile.Project, Configurations);
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
                property = MsBuildUtilities.GetProperty(projectFile.Project, Configurations);
                Assert.NotNull(property);
                Assert.Equal("Debug;Release", property.Value);
            }
        }

        [Fact]
        public async void ConfigurationProjectConfigurationDimensionProvider_OnDimensionValueChanged_Remove_MissingValue()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Delete,
                    ChangeEventStage.Before,
                    ConfigurationGeneral.ConfigurationProperty,
                    "NonExistantConfiguration");
                await Assert.ThrowsAsync<ArgumentException>(() => provider.OnDimensionValueChangedAsync(args));
                var property = MsBuildUtilities.GetProperty(projectFile.Project, Configurations);
                Assert.NotNull(property);
                Assert.Equal("Debug;Release;CustomConfiguration", property.Value);
            }
        }

        [Fact]
        public async void ConfigurationProjectConfigurationDimensionProvider_OnDimensionValueChanged_Rename()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                // On ChangeEventStage.Before nothing should be changed
                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Rename,
                    ChangeEventStage.Before,
                    ConfigurationGeneral.ConfigurationProperty,
                    "RenamedConfiguration",
                    "CustomConfiguration");
                await provider.OnDimensionValueChangedAsync(args);
                var property = MsBuildUtilities.GetProperty(projectFile.Project, Configurations);
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
                property = MsBuildUtilities.GetProperty(projectFile.Project, Configurations);
                Assert.NotNull(property);
                Assert.Equal("Debug;Release;RenamedConfiguration", property.Value);
            }
        }

        [Fact]
        public async void ConfigurationProjectConfigurationDimensionProvider_OnDimensionValueChanged_Rename_MissingValue()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Rename,
                    ChangeEventStage.After,
                    ConfigurationGeneral.ConfigurationProperty,
                    "RenamedConfiguration",
                    "NonExistantConfiguration");
                await Assert.ThrowsAsync<ArgumentException>(() => provider.OnDimensionValueChangedAsync(args));
                var property = MsBuildUtilities.GetProperty(projectFile.Project, Configurations);
                Assert.NotNull(property);
                Assert.Equal("Debug;Release;CustomConfiguration", property.Value);
            }
        }
    }
}
