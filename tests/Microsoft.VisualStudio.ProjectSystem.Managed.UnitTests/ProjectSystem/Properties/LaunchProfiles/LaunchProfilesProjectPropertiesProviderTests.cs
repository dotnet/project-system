// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class LaunchProfilesProjectPropertiesProviderTests
    {
        private const string DefaultTestProjectPath = @"C:\alpha\beta\gamma.csproj";

        private static readonly IEnumerable<Lazy<ILaunchProfileExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>> EmptyLaunchProfileExtensionValueProviders =
            Enumerable.Empty<Lazy<ILaunchProfileExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>>();
        private static readonly IEnumerable<Lazy<IGlobalSettingExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>> EmptyGlobalSettingExtensionValueProviders = 
            Enumerable.Empty<Lazy<IGlobalSettingExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>>();

        [Fact]
        public void DefaultProjectPath_IsTheProjectPath()
        {
            var provider = new LaunchProfileProjectPropertiesProvider(
                CreateDefaultTestProject(),
                ILaunchSettingsProviderFactory.Create(),
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var defaultProjectPath = provider.DefaultProjectPath;
            Assert.Equal(expected: DefaultTestProjectPath, actual: defaultProjectPath);
        }

        [Fact]
        public void WhenRetrievingProjectLevelProperties_NullIsReturned()
        {
            var project = UnconfiguredProjectFactory.Create();
            var provider = new LaunchProfileProjectPropertiesProvider(
                project,
                ILaunchSettingsProviderFactory.Create(),
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var commonProperties = provider.GetCommonProperties();

            Assert.Null(commonProperties);
        }

        [Fact]
        public void WhenRetrievingItemTypeProperties_NullIsReturned()
        {
            var project = UnconfiguredProjectFactory.Create();
            var provider = new LaunchProfileProjectPropertiesProvider(
                project,
                ILaunchSettingsProviderFactory.Create(),
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var itemTypeProperties = provider.GetItemTypeProperties(LaunchProfileProjectItemProvider.ItemType);

            Assert.Null(itemTypeProperties);
        }

        [Fact]
        public void WhenRetrievingItemProperties_NullIsReturnedIfTheItemIsNull()
        {
            var project = UnconfiguredProjectFactory.Create();
            var provider = new LaunchProfileProjectPropertiesProvider(
                project,
                ILaunchSettingsProviderFactory.Create(),
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var itemProperties = provider.GetItemProperties(LaunchProfileProjectItemProvider.ItemType, item: null);

            Assert.Null(itemProperties);
        }

        [Fact]
        public void WhenRetrievingItemProperties_PropertiesAreReturnedIfTheItemTypeMatches()
        {
            var provider = new LaunchProfileProjectPropertiesProvider(
                UnconfiguredProjectFactory.Create(),
                CreateDefaultTestLaunchSettings(),
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var properties = provider.GetItemProperties(
                itemType: LaunchProfileProjectItemProvider.ItemType,
                item: "Profile1");

            Assert.NotNull(properties);
        }

        [Fact]
        public void WhenRetrievingItemProperties_PropertiesAreReturnedIfTheItemTypeIsNull()
        {
            var provider = new LaunchProfileProjectPropertiesProvider(
                UnconfiguredProjectFactory.Create(),
                CreateDefaultTestLaunchSettings(),
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var properties = provider.GetItemProperties(
                itemType: null,
                item: "Profile1");

            Assert.NotNull(properties);
        }

        [Fact]
        public void WhenRetrievingItemProperties_NullIsReturnedIfTheItemTypeDoesNotMatch()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });

            var provider = new LaunchProfileProjectPropertiesProvider(
                UnconfiguredProjectFactory.Create(),
                launchSettingsProvider,
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var properties = provider.GetItemProperties(
                itemType: "RandomItemType",
                item: "Profile1");

            Assert.Null(properties);
        }

        [Fact]
        public void WhenRetrievingItemProperties_NullIsReturnedWhenTheFilePathIsNotTheProjectPath()
        {
            var provider = new LaunchProfileProjectPropertiesProvider(
                CreateDefaultTestProject(),
                CreateDefaultTestLaunchSettings(),
                EmptyLaunchProfileExtensionValueProviders,
                EmptyGlobalSettingExtensionValueProviders);

            var properties = provider.GetProperties(
                file: @"C:\sigma\lambda\other.csproj",
                itemType: LaunchProfileProjectItemProvider.ItemType,
                item: "Profile1");

            Assert.Null(properties);
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
        private static ILaunchSettingsProvider3 CreateDefaultTestLaunchSettings()
        {
            var profile1 = new WritableLaunchProfile { Name = "Profile1" };
            var profile2 = new WritableLaunchProfile { Name = "Profile2" };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: new[] { profile1.ToLaunchProfile(), profile2.ToLaunchProfile() });
            return launchSettingsProvider;
        }
    }
}
