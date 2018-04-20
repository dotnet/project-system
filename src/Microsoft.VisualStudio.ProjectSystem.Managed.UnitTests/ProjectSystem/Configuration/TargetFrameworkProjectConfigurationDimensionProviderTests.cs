// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Build;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    [Trait("UnitTest", "ProjectSystem")]
    public class TargetFrameworkProjectConfigurationDimensionProviderTests
    {
        private const string ProjectXmlTFM =
@"<Project>
  <PropertyGroup>
    <TargetFramework>netcoreapp1.0</TargetFramework>
  </PropertyGroup>
</Project>";

        private const string ProjectXmlTFMs =
@"<Project>
  <PropertyGroup>
    <TargetFrameworks>netcoreapp1.0;net45</TargetFrameworks>
  </PropertyGroup>
</Project>";

        private const string ProjectXmlTFMAndTFMs =
@"<Project>
  <PropertyGroup>
    <TargetFrameworks>netcoreapp1.0;net45</TargetFrameworks>
    <TargetFramework>netcoreapp1.0</TargetFramework>
  </PropertyGroup>
</Project>";

        [Fact]
        public async Task GetDefaultValuesForDimensionsAsync_TFM()
        {
            var projectAccessor = IProjectAccessorFactory.Create(ProjectXmlTFM);
            var provider = new TargetFrameworkProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);

            Assert.Empty(values);
        }

        [Theory]
        [InlineData(ProjectXmlTFMs)]
        [InlineData(ProjectXmlTFMAndTFMs)]
        public async Task GetDefaultValuesForDimensionsAsync_TFMs(string projectXml)
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);
            var provider = new TargetFrameworkProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);

            Assert.Single(values);
            var value = values.First();
            Assert.Equal(ConfigurationGeneral.TargetFrameworkProperty, value.Key);
            Assert.Equal("netcoreapp1.0", value.Value);
        }

        [Fact]
        public async Task GetProjectConfigurationDimensionsAsync_TFM()
        {
            var projectAccessor = IProjectAccessorFactory.Create(ProjectXmlTFM);
            var provider = new TargetFrameworkProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);

            Assert.Empty(values);
        }

        [Theory]
        [InlineData(ProjectXmlTFMs)]
        [InlineData(ProjectXmlTFMAndTFMs)]
        public async Task GetProjectConfigurationDimensionsAsync_TFMs(string projectXml)
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);
            var provider = new TargetFrameworkProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);

            Assert.Single(values);
            var value = values.First();
            Assert.Equal(ConfigurationGeneral.TargetFrameworkProperty, value.Key);
            string[] dimensionValues = value.Value.ToArray();
            AssertEx.CollectionLength(dimensionValues, 2);
            Assert.Equal("netcoreapp1.0", dimensionValues[0]);
            Assert.Equal("net45", dimensionValues[1]);
        }

        [Theory]
        [InlineData(ConfigurationDimensionChange.Add, ChangeEventStage.Before)]
        [InlineData(ConfigurationDimensionChange.Add, ChangeEventStage.After)]
        [InlineData(ConfigurationDimensionChange.Rename, ChangeEventStage.Before)]
        [InlineData(ConfigurationDimensionChange.Rename, ChangeEventStage.After)]
        [InlineData(ConfigurationDimensionChange.Delete, ChangeEventStage.Before)]
        [InlineData(ConfigurationDimensionChange.Delete, ChangeEventStage.After)]
        public async Task OnDimensionValueChanged(ConfigurationDimensionChange change, ChangeEventStage stage)
        {
            // No changes should happen for TFM so verify that the property is the same before and after
            var rootElement = ProjectRootElementFactory.Create(ProjectXmlTFMs);
            var projectAccessor = IProjectAccessorFactory.Create(rootElement);
            var provider = new TargetFrameworkProjectConfigurationDimensionProvider(projectAccessor);

            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var property = BuildUtilities.GetProperty(rootElement, ConfigurationGeneral.TargetFrameworksProperty);
            string expectedTFMs = property.Value;

            var args = new ProjectConfigurationDimensionValueChangedEventArgs(
                unconfiguredProject,
                change,
                stage,
                ConfigurationGeneral.TargetFrameworkProperty,
                "NewTFM");
            await provider.OnDimensionValueChangedAsync(args);

            Assert.NotNull(property);
            Assert.Equal(expectedTFMs, property.Value);
        }

        [Theory]
        [InlineData("net45",                "net45")]
        [InlineData(" net45 ",              "net45")]
        [InlineData("net46",                "net46")]
        [InlineData("net45;net46",          "net45")]
        [InlineData(";net45;net46",         "net45")]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_ReturnsFirstValue(string frameworks, string expected)
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
    <TargetFrameworks>{frameworks}</TargetFrameworks>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Single(result);
            Assert.Equal("TargetFramework", result.First().Key);
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
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenTargetFrameworksIsEmpty_ReturnsEmpty(string frameworks)
        {
            string projectXml =
$@"<Project>
  <PropertyGroup>
    <TargetFrameworks>{frameworks}</TargetFrameworks>
  </PropertyGroup>
</Project>";

            var provider = CreateInstance(projectXml);

            var result = await provider.GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProjectFactory.Create());

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetBestGuessDefaultValuesForDimensionsAsync_WhenTargetFrameworksIsMissing_ReturnsEmpty()
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

        private static TargetFrameworkProjectConfigurationDimensionProvider CreateInstance(string projectXml)
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);

            return new TargetFrameworkProjectConfigurationDimensionProvider(projectAccessor);
        }
    }
}
