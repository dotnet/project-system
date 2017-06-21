// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.Mocks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
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
        public async Task PlatformProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var telemetryService = ITelemetryServiceFactory.Implement();
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new PlatformProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);
                Assert.Equal(1, values.Count());
                var value = values.First();
                Assert.Equal(ConfigurationGeneral.PlatformProperty, value.Key);
                Assert.Equal("AnyCPU", value.Value);
            }
        }

        [Fact]
        public async Task PlatformProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var telemetryService = ITelemetryServiceFactory.Implement();
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new PlatformProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
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
        public async Task PlatformProjectConfigurationDimensionProvider_OnDimensionValueChanged_Add()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var postPropertyEvents = new List<Tuple<string, string, string>>();
                var telemetryService = ITelemetryServiceFactory.Implement(postProperty: postPropertyEvents.Add);
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new PlatformProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
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

                var telemetryEvent = postPropertyEvents[0];
                Assert.Equal("DimensionChanged/Platform/Add", telemetryEvent.Item1);
                Assert.Equal("Value", telemetryEvent.Item2);
                Assert.Equal("ARM#Hashed", telemetryEvent.Item3);
            }
        }

        [Fact]
        public async Task PlatformProjectConfigurationDimensionProvider_OnDimensionValueChanged_Remove()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var postPropertyEvents = new List<Tuple<string, string, string>>();
                var telemetryService = ITelemetryServiceFactory.Implement(postProperty: postPropertyEvents.Add);
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new PlatformProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
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

                var telemetryEvent = postPropertyEvents[0];
                Assert.Equal("DimensionChanged/Platform/Remove", telemetryEvent.Item1);
                Assert.Equal("Value", telemetryEvent.Item2);
                Assert.Equal("x86#Hashed", telemetryEvent.Item3);
            }
        }

        [Fact]
        public async Task PlatformProjectConfigurationDimensionProvider_OnDimensionValueChanged_Rename()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var telemetryService = ITelemetryServiceFactory.Implement();
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new PlatformProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
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
