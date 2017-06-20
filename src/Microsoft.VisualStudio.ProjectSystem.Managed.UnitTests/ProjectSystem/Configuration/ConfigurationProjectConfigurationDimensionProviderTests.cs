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
        public async Task ConfigurationProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var telemetryService = ITelemetryServiceFactory.Implement();
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);
                Assert.Equal(1, values.Count());
                var value = values.First();
                Assert.Equal(ConfigurationGeneral.ConfigurationProperty, value.Key);
                Assert.Equal("Debug", value.Value);
            }
        }

        [Fact]
        public async Task ConfigurationProjectConfigurationDimensionProvider_GetDefaultValuesForDimensionsAsync_NoPropertyValue()
        {
            using (var projectFile = new MsBuildProjectFile())
            {
                var telemetryService = ITelemetryServiceFactory.Implement();
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetDefaultValuesForDimensionsAsync(unconfiguredProject);
                Assert.Equal(0, values.Count());
            }
        }

        [Fact]
        public async Task ConfigurationProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var telemetryService = ITelemetryServiceFactory.Implement();
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
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

        public async Task ConfigurationProjectConfigurationDimensionProvider_GetProjectConfigurationDimensionsAsync_NoPropertyValue()
        {
            using (var projectFile = new MsBuildProjectFile())
            {
                var telemetryService = ITelemetryServiceFactory.Implement();
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);
                var values = await provider.GetProjectConfigurationDimensionsAsync(unconfiguredProject);
                Assert.Equal(0, values.Count());
            }
        }

        [Fact]
        public async Task ConfigurationProjectConfigurationDimensionProvider_OnDimensionValueChanged_Add()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var postPropertyEvents = new List<Tuple<string, string, string>>();
                var telemetryService = ITelemetryServiceFactory.Implement(postProperty: postPropertyEvents.Add);
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);

                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                // On ChangeEventStage.After nothing should be changed
                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Add,
                    ChangeEventStage.After,
                    ConfigurationGeneral.ConfigurationProperty,
                    "CustomConfig");
                await provider.OnDimensionValueChangedAsync(args);
                var property = BuildUtilities.GetProperty(projectFile.Project, Configurations);
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
                property = BuildUtilities.GetProperty(projectFile.Project, Configurations);
                Assert.NotNull(property);
                Assert.Equal("Debug;Release;CustomConfiguration;CustomConfig", property.Value);

                var telemetryEvent = postPropertyEvents[0];
                Assert.Equal("DimensionChanged/Configuration/Add", telemetryEvent.Item1);
                Assert.Equal("Value", telemetryEvent.Item2);
                Assert.Equal("CustomConfig#Hashed", telemetryEvent.Item3);
            }
        }

        [Fact]
        public async Task ConfigurationProjectConfigurationDimensionProvider_OnDimensionValueChanged_Remove()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var postPropertyEvents = new List<Tuple<string, string, string>>();
                var telemetryService = ITelemetryServiceFactory.Implement(postProperty: postPropertyEvents.Add);
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                // On ChangeEventStage.After nothing should be changed
                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Delete,
                    ChangeEventStage.After,
                    ConfigurationGeneral.ConfigurationProperty,
                    "CustomConfiguration");
                await provider.OnDimensionValueChangedAsync(args);
                var property = BuildUtilities.GetProperty(projectFile.Project, Configurations);
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
                property = BuildUtilities.GetProperty(projectFile.Project, Configurations);
                Assert.NotNull(property);
                Assert.Equal("Debug;Release", property.Value);

                var telemetryEvent = postPropertyEvents[0];
                Assert.Equal("DimensionChanged/Configuration/Remove", telemetryEvent.Item1);
                Assert.Equal("Value", telemetryEvent.Item2);
                Assert.Equal("CustomConfiguration#Hashed", telemetryEvent.Item3);
            }
        }

        [Fact]
        public async Task ConfigurationProjectConfigurationDimensionProvider_OnDimensionValueChanged_Remove_MissingValue()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var telemetryService = ITelemetryServiceFactory.Implement();
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Delete,
                    ChangeEventStage.Before,
                    ConfigurationGeneral.ConfigurationProperty,
                    "NonExistantConfiguration");
                await Assert.ThrowsAsync<ArgumentException>(() => provider.OnDimensionValueChangedAsync(args));
                var property = BuildUtilities.GetProperty(projectFile.Project, Configurations);
                Assert.NotNull(property);
                Assert.Equal("Debug;Release;CustomConfiguration", property.Value);
            }
        }

        [Fact]
        public async Task ConfigurationProjectConfigurationDimensionProvider_OnDimensionValueChanged_Rename()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var postPropertiesEvents = new List<Tuple<string, List<(string propertyName, string propertyValue)>>>();
                var telemetryService = ITelemetryServiceFactory.Implement(postProperties: postPropertiesEvents.Add);
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
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
                var property = BuildUtilities.GetProperty(projectFile.Project, Configurations);
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
                property = BuildUtilities.GetProperty(projectFile.Project, Configurations);
                Assert.NotNull(property);
                Assert.Equal("Debug;Release;RenamedConfiguration", property.Value);

                var telemetryEvent = postPropertiesEvents[0];
                Assert.Equal("DimensionChanged/Configuration/Rename", telemetryEvent.Item1);
                Assert.Equal("OldValue", telemetryEvent.Item2[0].propertyName);
                Assert.Equal("CustomConfiguration#Hashed", telemetryEvent.Item2[0].propertyValue);
                Assert.Equal("NewValue", telemetryEvent.Item2[1].propertyName);
                Assert.Equal("RenamedConfiguration#Hashed", telemetryEvent.Item2[1].propertyValue);
            }
        }

        [Fact]
        public async Task ConfigurationProjectConfigurationDimensionProvider_OnDimensionValueChanged_Rename_MissingValue()
        {
            using (var projectFile = new MsBuildProjectFile(projectXml))
            {
                var telemetryService = ITelemetryServiceFactory.Implement();
                IProjectXmlAccessor _projectXmlAccessor = IProjectXmlAccessorFactory.Create(projectFile.Project);
                var provider = new ConfigurationProjectConfigurationDimensionProvider(_projectXmlAccessor, telemetryService);
                var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projectFile.Filename);

                ProjectConfigurationDimensionValueChangedEventArgs args = new ProjectConfigurationDimensionValueChangedEventArgs(
                    unconfiguredProject,
                    ConfigurationDimensionChange.Rename,
                    ChangeEventStage.After,
                    ConfigurationGeneral.ConfigurationProperty,
                    "RenamedConfiguration",
                    "NonExistantConfiguration");
                await Assert.ThrowsAsync<ArgumentException>(() => provider.OnDimensionValueChangedAsync(args));
                var property = BuildUtilities.GetProperty(projectFile.Project, Configurations);
                Assert.NotNull(property);
                Assert.Equal("Debug;Release;CustomConfiguration", property.Value);
            }
        }
    }
}
