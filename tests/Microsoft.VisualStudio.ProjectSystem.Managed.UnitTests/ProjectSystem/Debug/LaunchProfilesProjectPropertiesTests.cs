// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchProfilesProjectPropertiesTests
    {
        private const string DefaultTestProjectPath = @"C:\alpha\beta\gamma.csproj";

        private static readonly IEnumerable<Lazy<ILaunchProfileExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>> EmptyLaunchProfileExtensionValueProviders =
            Enumerable.Empty<Lazy<ILaunchProfileExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>>();
        private static readonly IEnumerable<Lazy<IGlobalSettingExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>> EmptyGlobalSettingExtensionValueProviders =
            Enumerable.Empty<Lazy<IGlobalSettingExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>>();

        [Fact]
        public void WhenRetrievingItemProperties_TheContextHasTheExpectedValues()
        {
            var provider = new LaunchProfileProjectPropertiesProvider(
                CreateDefaultTestProject(),
                CreateDefaultTestLaunchSettings(),
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var properties = provider.GetItemProperties(
                itemType: LaunchProfileProjectItemProvider.ItemType,
                item: "Profile1");
            var context = properties.Context;

            Assert.Equal(expected: DefaultTestProjectPath, actual: context.File);
            Assert.True(context.IsProjectFile);
            Assert.Equal(expected: "Profile1", actual: context.ItemName);
            Assert.Equal(expected: LaunchProfileProjectItemProvider.ItemType, actual: context.ItemType);
        }

        [Fact]
        public void WhenRetrievingItemProperties_TheFilePathIsTheProjectPath()
        {
            var provider = new LaunchProfileProjectPropertiesProvider(
                CreateDefaultTestProject(),
                CreateDefaultTestLaunchSettings(),
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var properties = provider.GetItemProperties(
                itemType: LaunchProfileProjectItemProvider.ItemType,
                item: "Profile1");

            Assert.Equal(expected: DefaultTestProjectPath, actual: properties.FileFullPath);
        }

        [Fact]
        public void WhenRetrievingItemProperties_ThePropertyKindIsItemGroup()
        {
            var provider = new LaunchProfileProjectPropertiesProvider(
                CreateDefaultTestProject(),
                CreateDefaultTestLaunchSettings(),
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var properties = provider.GetItemProperties(
                itemType: LaunchProfileProjectItemProvider.ItemType,
                item: "Profile1");

            Assert.Equal(expected: PropertyKind.ItemGroup, actual: properties.PropertyKind);
        }

        [Fact]
        public async Task WhenRetrievingItemPropertyNames_AllStandardProfilePropertyNamesAreReturnedEvenIfNotDefined()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile() });

            var provider = new LaunchProfileProjectPropertiesProvider(
                CreateDefaultTestProject(),
                launchSettingsProvider,
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);
            var properties = provider.GetItemProperties(
                itemType: LaunchProfileProjectItemProvider.ItemType,
                item: "Profile1");
            var propertyNames = await properties.GetPropertyNamesAsync();

            Assert.Contains("CommandName", propertyNames);
            Assert.Contains("ExecutablePath", propertyNames);
            Assert.Contains("CommandLineArguments", propertyNames);
            Assert.Contains("WorkingDirectory", propertyNames);
            Assert.Contains("LaunchBrowser", propertyNames);
            Assert.Contains("LaunchUrl", propertyNames);
            Assert.Contains("EnvironmentVariables", propertyNames);
        }

        [Fact]
        public async Task WhenRetrievingStandardPropertyValues_TheEmptyStringIsReturnedForUndefinedProperties()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile() });

            var provider = new LaunchProfileProjectPropertiesProvider(
                CreateDefaultTestProject(),
                launchSettingsProvider,
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);
            var properties = provider.GetItemProperties(
                itemType: LaunchProfileProjectItemProvider.ItemType,
                item: "Profile1");

            var standardPropertyNames = new[]
            {
                "CommandName",
                "ExecutablePath",
                "CommandLineArguments",
                "WorkingDirectory",
                "LaunchUrl",
                "EnvironmentVariables"
            };

            foreach (var standardPropertyName in standardPropertyNames)
            {
                var evaluatedValue = await properties.GetEvaluatedPropertyValueAsync(standardPropertyName);
                Assert.Equal(expected: string.Empty, actual: evaluatedValue);
                var unevaluatedValue = await properties.GetUnevaluatedPropertyValueAsync(standardPropertyName);
                Assert.Equal(expected: string.Empty, actual: unevaluatedValue);
            }
        }

        [Fact]
        public async Task WhenRetrievingTheLaunchBrowserValue_TheDefaultValueIsFalse()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile() });

            var provider = new LaunchProfileProjectPropertiesProvider(
                CreateDefaultTestProject(),
                launchSettingsProvider,
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);
            var properties = provider.GetItemProperties(
                itemType: LaunchProfileProjectItemProvider.ItemType,
                item: "Profile1");

            var evaluatedValue = await properties.GetEvaluatedPropertyValueAsync("LaunchBrowser");
            Assert.Equal(expected: "false", actual: evaluatedValue);
            var unevaluatedValue = await properties.GetUnevaluatedPropertyValueAsync("LaunchBrowser");
            Assert.Equal(expected: "false", actual: unevaluatedValue);
        }

        [Fact]
        public async Task WhenRetrievingStandardPropertyValues_TheExpectedValuesAreReturned()
        {
            var profile1 = new WritableLaunchProfile
            {
                Name = "Profile1",
                CommandLineArgs = "alpha beta gamma",
                CommandName = "epsilon",
                EnvironmentVariables = { ["One"] = "1", ["Two"] = "2" },
                ExecutablePath = @"D:\five\six\seven\eight.exe",
                LaunchBrowser = true,
                LaunchUrl = "https://localhost/profile",
                WorkingDirectory = @"C:\users\other\temp"
            };

            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile() });
            var provider = new LaunchProfileProjectPropertiesProvider(
                CreateDefaultTestProject(),
                launchSettingsProvider,
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);
            var properties = provider.GetItemProperties(
                itemType: LaunchProfileProjectItemProvider.ItemType,
                item: "Profile1");

            var expectedValues = new Dictionary<string, string>
            {
                ["CommandLineArguments"] = "alpha beta gamma",
                ["CommandName"] = "epsilon",
                ["EnvironmentVariables"] = "One=1,Two=2",
                ["ExecutablePath"] = @"D:\five\six\seven\eight.exe",
                ["LaunchBrowser"] = "true",
                ["LaunchUrl"] = "https://localhost/profile",
                ["WorkingDirectory"] = @"C:\users\other\temp",
            };

            foreach (var (propertyName, expectedPropertyValue) in expectedValues)
            {
                var actualUnevaluatedValue = await properties.GetUnevaluatedPropertyValueAsync(propertyName);
                var actualEvaluatedValue = await properties.GetEvaluatedPropertyValueAsync(propertyName);
                Assert.Equal(expectedPropertyValue, actualUnevaluatedValue);
                Assert.Equal(expectedPropertyValue, actualEvaluatedValue);
            }
        }

        /// <summary>
        /// Creates an <see cref="UnconfiguredProject"/> where the <see cref="UnconfiguredProject.FullPath"/>
        /// is set to <see cref="DefaultTestProjectPath"/>.
        /// </summary>
        private static UnconfiguredProject CreateDefaultTestProject()
        {
            return UnconfiguredProjectFactory.ImplementFullPath(DefaultTestProjectPath);
        }

        /// <summary>
        /// Creates an <see cref="ILaunchSettingsProvider"/> with two empty profiles named
        /// "Profile1" and "Profile2".
        /// </summary>
        private static ILaunchSettingsProvider CreateDefaultTestLaunchSettings()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });
            return launchSettingsProvider;
        }
    }
}
