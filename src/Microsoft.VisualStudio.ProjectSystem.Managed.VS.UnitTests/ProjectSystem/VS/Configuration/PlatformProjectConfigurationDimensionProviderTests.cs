// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Configuration
{
    [ProjectSystemTrait]
    public class PlatformProjectConfigurationDimensionProviderTests
    {
        const string Platforms = nameof(Platforms);

        private string projectXml =
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <Platforms>AnyCPU;x64;x86</Platforms>
  </PropertyGroup>
</Project>";

        [Fact]
        public async void PlatformProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new PlatformProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);
                Assert.Equal(1, values.Count());
                var value = values.First();
                Assert.Equal(ConfigurationGeneral.PlatformProperty, value.Key);
                Assert.Equal("AnyCPU", value.Value);
            }
        }

        [Fact]
        public async void PlatformProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new PlatformProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);
                Assert.Equal(1, values.Count());
                var value = values.First();
                Assert.Equal(ConfigurationGeneral.PlatformProperty, value.Key);
                string[] dimensionValues = value.Value.ToArray();
                Assert.Equal(3, dimensionValues.Length);
                Assert.Equal("AnyCPU", dimensionValues[0]);
                Assert.Equal("x64", dimensionValues[1]);
                Assert.Equal("x86", dimensionValues[2]);
            }
        }

        [Fact]
        public async void PlatformProjectConfigurationDimensionProvider_OnDimensionValueChanged_Add()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new PlatformProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                // On ChangeEventStage.After nothing should be changed
                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Add,
                    ChangeEventStage.After,
                    ConfigurationGeneral.PlatformProperty,
                    "ARM");
                await provider.OnDimensionValueChangedAsync(args);
                var property = BuildUtilities.GetProperty(projectFile.Project, Platforms);
                Assert.NotNull(property);
                Assert.Equal("AnyCPU;x64;x86", property.Value);

                // On ChangeEventStage.Before the property should be added
                args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Add,
                    ChangeEventStage.Before,
                    ConfigurationGeneral.PlatformProperty,
                    "ARM");
                await provider.OnDimensionValueChangedAsync(args);
                property = BuildUtilities.GetProperty(projectFile.Project, Platforms);
                Assert.NotNull(property);
                Assert.Equal("AnyCPU;x64;x86;ARM", property.Value);
            }
        }

        [Fact]
        public async void PlatformProjectConfigurationDimensionProvider_OnDimensionValueChanged_Remove()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new PlatformProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                // On ChangeEventStage.After nothing should be changed
                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Delete,
                    ChangeEventStage.After,
                    ConfigurationGeneral.PlatformProperty,
                    "x86");
                await provider.OnDimensionValueChangedAsync(args);
                var property = BuildUtilities.GetProperty(projectFile.Project, Platforms);
                Assert.NotNull(property);
                Assert.Equal("AnyCPU;x64;x86", property.Value);

                // On ChangeEventStage.Before the property should be removed
                args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Delete,
                    ChangeEventStage.Before,
                    ConfigurationGeneral.PlatformProperty,
                    "x86");
                await provider.OnDimensionValueChangedAsync(args);
                property = BuildUtilities.GetProperty(projectFile.Project, Platforms);
                Assert.NotNull(property);
                Assert.Equal("AnyCPU;x64", property.Value);
            }
        }

        [Fact]
        public async void PlatformProjectConfigurationDimensionProvider_OnDimensionValueChanged_Rename()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new PlatformProjectConfigurationDimensionProvider(_projectXmlAccessor);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                // Nothing should happen on platform rename as it's unsupported
                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Rename,
                    ChangeEventStage.Before,
                    ConfigurationGeneral.PlatformProperty,
                    "RenamedPlatform",
                    "x86");
                await provider.OnDimensionValueChangedAsync(args);
                var property = BuildUtilities.GetProperty(projectFile.Project, Platforms);
                Assert.NotNull(property);
                Assert.Equal("AnyCPU;x64;x86", property.Value);

                // On ChangeEventStage.Before the property should be renamed
                args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Rename,
                    ChangeEventStage.After,
                    ConfigurationGeneral.PlatformProperty,
                    "RenamedPlatform",
                    "x86");
                await provider.OnDimensionValueChangedAsync(args);
                property = BuildUtilities.GetProperty(projectFile.Project, Platforms);
                Assert.NotNull(property);
                Assert.Equal("AnyCPU;x64;x86", property.Value);
            }
        }
    }
}
