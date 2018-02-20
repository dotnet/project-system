// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Build;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    [Trait("UnitTest", "ProjectSystem")]
    public class PlatformProjectConfigurationDimensionProviderTests
    {
        private const string Platforms = nameof(Platforms);

        private string projectXml =
@"<Project>
  <PropertyGroup>
    <Platforms>AnyCPU;x64;x86</Platforms>
  </PropertyGroup>
</Project>";

        [Fact]
        public async Task PlatformProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync()
        {
            var project = ProjectRootElementFactory.Create(projectXml);
            var projectXmlAccessor = IProjectXmlAccessorFactory.Create(project);
            var provider = new PlatformProjectConfigurationDimensionProvider(projectXmlAccessor);
            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);
            Assert.Single(values);
            var value = values.First();
            Assert.Equal(ConfigurationGeneral.PlatformProperty, value.Key);
            Assert.Equal("AnyCPU", value.Value);
        }

        [Fact]
        public async Task PlatformProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync()
        {
            var project = ProjectRootElementFactory.Create(projectXml);
            var projectXmlAccessor = IProjectXmlAccessorFactory.Create(project);
            var provider = new PlatformProjectConfigurationDimensionProvider(projectXmlAccessor);
            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);
            Assert.Single(values);
            var value = values.First();
            Assert.Equal(ConfigurationGeneral.PlatformProperty, value.Key);
            string[] dimensionValues = value.Value.ToArray();
            Assert.Equal(3, dimensionValues.Length);
            Assert.Equal("AnyCPU", dimensionValues[0]);
            Assert.Equal("x64", dimensionValues[1]);
            Assert.Equal("x86", dimensionValues[2]);
        }

        [Fact]
        public async Task PlatformProjectConfigurationDimensionProvider_OnDimensionValueChanged_Add()
        {
            var project = ProjectRootElementFactory.Create(projectXml);
            var projectXmlAccessor = IProjectXmlAccessorFactory.Create(project);
            var provider = new PlatformProjectConfigurationDimensionProvider(projectXmlAccessor);
            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            // On ChangeEventStage.After nothing should be changed
            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Add,
                ChangeEventStage.After,
                ConfigurationGeneral.PlatformProperty,
                "ARM");
            await provider.OnDimensionValueChangedAsync(args);
            var property = BuildUtilities.GetProperty(project, Platforms);
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
            property = BuildUtilities.GetProperty(project, Platforms);
            Assert.NotNull(property);
            Assert.Equal("AnyCPU;x64;x86;ARM", property.Value);
        }

        [Fact]
        public async Task PlatformProjectConfigurationDimensionProvider_OnDimensionValueChanged_Remove()
        {
            var project = ProjectRootElementFactory.Create(projectXml);
            var projectXmlAccessor = IProjectXmlAccessorFactory.Create(project);
            var provider = new PlatformProjectConfigurationDimensionProvider(projectXmlAccessor);
            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            // On ChangeEventStage.After nothing should be changed
            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Delete,
                ChangeEventStage.After,
                ConfigurationGeneral.PlatformProperty,
                "x86");
            await provider.OnDimensionValueChangedAsync(args);
            var property = BuildUtilities.GetProperty(project, Platforms);
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
            property = BuildUtilities.GetProperty(project, Platforms);
            Assert.NotNull(property);
            Assert.Equal("AnyCPU;x64", property.Value);
        }

        [Fact]
        public async Task PlatformProjectConfigurationDimensionProvider_OnDimensionValueChanged_Rename()
        {
            var project = ProjectRootElementFactory.Create(projectXml);
            var projectXmlAccessor = IProjectXmlAccessorFactory.Create(project);
            var provider = new PlatformProjectConfigurationDimensionProvider(projectXmlAccessor);
            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            // Nothing should happen on platform rename as it's unsupported
            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                ConfigurationDimensionChange.Rename,
                ChangeEventStage.Before,
                ConfigurationGeneral.PlatformProperty,
                "RenamedPlatform",
                "x86");
            await provider.OnDimensionValueChangedAsync(args);
            var property = BuildUtilities.GetProperty(project, Platforms);
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
            property = BuildUtilities.GetProperty(project, Platforms);
            Assert.NotNull(property);
            Assert.Equal("AnyCPU;x64;x86", property.Value);
        }
    }
}
