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
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);
            var provider = new PlatformProjectConfigurationDimensionProvider(projectAccessor);

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
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);
            var provider = new PlatformProjectConfigurationDimensionProvider(projectAccessor);

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
            var projectAccessor = IProjectAccessorFactory.Create(project);
            var provider = new PlatformProjectConfigurationDimensionProvider(projectAccessor);

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
            var projectAccessor = IProjectAccessorFactory.Create(project);
            var provider = new PlatformProjectConfigurationDimensionProvider(projectAccessor);

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
            var projectAccessor = IProjectAccessorFactory.Create(project);
            var provider = new PlatformProjectConfigurationDimensionProvider(projectAccessor);

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

        [Theory]
        [InlineData("ARM",                  "ARM")]
        [InlineData(" ARM ",                "ARM")]
        [InlineData("x64",                  "x64")]
        [InlineData("ARM;",                 "ARM")]
        [InlineData("ARM;x64",              "ARM")]
        [InlineData(";ARM;x64",             "ARM")]
        [InlineData("$(Foo);ARM;x64",       "ARM")]
        [InlineData("$(Foo); ARM ;x64",     "ARM")]
        [InlineData("x64_$(Foo); ARM ;x64", "ARM")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_ReturnsFirstParsableValue(string platforms, string expected)
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
    <Platforms>{platforms}</Platforms>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Platform", result.First().Key);
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
        [InlineData("Foo_$(Property);")]
        [InlineData(";Foo_$(Property);")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenPlatformsIsEmpty_ReturnsDefault(string platforms)
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
    <Platforms>{platforms}</Platforms>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Platform", result.First().Key);
            Assert.Equal("AnyCPU", result.First().Value);
        }

        [Fact]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_ReturnsFirstValueFromLastPlatformsElement()
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
    <Platforms>x64</Platforms>
    <Platforms>ARM;x86</Platforms>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Platform", result.First().Key);
            Assert.Equal("ARM", result.First().Value);
        }

        [Fact]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenPlatformsIsMissing_ReturnsDefault()
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Platform", result.First().Key);
            Assert.Equal("AnyCPU", result.First().Value);
        }

        [Theory]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Platforms Condition=""'$(BuildingInsideVisualStudio)' != 'true'"">ARM</Platforms>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Platforms Condition=""'$(OS)' != 'Windows_NT'"">ARM</Platforms>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Platforms Condition=""'$(OS)' == 'Unix'"">ARM</Platforms>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Platforms Condition=""'$(Foo)' == 'true'"">ARM</Platforms>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Platform>ARM</Platform>
    <Platforms Condition=""'$(OS)' != 'Windows_NT'"">ARM</Platforms>
  </PropertyGroup>
</Project>")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenPlatformsHasUnrecognizedCondition_ReturnsDefault(string projectXml)
        {
            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Platform", result.First().Key);
            Assert.Equal("AnyCPU", result.First().Value);
        }


        [Theory]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Platforms Condition=""'$(BuildingInsideVisualStudio)' == 'true'"">ARM</Platforms>
    <Platforms Condition=""'$(BuildingInsideVisualStudio)' != 'true'"">x86</Platforms>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Platforms Condition=""'$(OS)' == 'Windows_NT'"">ARM</Platforms>
    <Platforms Condition=""'$(OS)' != 'Windows_NT'"">x86</Platforms>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Platforms Condition=""'$(OS)' == 'Windows_NT'"">ARM</Platforms>
    <Platforms Condition=""'$(OS)' == 'Unix'"">x86</Platforms>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Platforms Condition=""true"">ARM</Platforms>
  </PropertyGroup>
</Project>")]
        [InlineData(
@"<Project>
  <PropertyGroup>
    <Platforms Condition="""">ARM</Platforms>
  </PropertyGroup>
</Project>")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenPlatformsHasRecognizedCondition_ReturnsValue(string projectXml)
        {
            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("Platform", result.First().Key);
            Assert.Equal("ARM", result.First().Value);
        }

        private static PlatformProjectConfigurationDimensionProvider CreateInstance(string projectXml)
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);

            return new PlatformProjectConfigurationDimensionProvider(projectAccessor);
        }
    }
}
