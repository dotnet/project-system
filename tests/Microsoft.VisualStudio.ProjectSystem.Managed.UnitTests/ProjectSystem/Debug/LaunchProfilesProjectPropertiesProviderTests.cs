// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchProfilesProjectPropertiesProviderTests
    {
        [Fact]
        public void DefaultProjectPath_IsTheProjectPath()
        {
            string projectPath = @"C:\alpha\beta\gamma.csproj";
            var project = UnconfiguredProjectFactory.ImplementFullPath(projectPath);

            var provider = new LaunchProfilesProjectPropertiesProvider(project);

            var defaultProjectPath = provider.DefaultProjectPath;
            Assert.Equal(expected: projectPath, actual: defaultProjectPath);
        }

        [Fact]
        public void WhenRetrievingProjectLevelProperties_NullIsReturned()
        {
            var project = UnconfiguredProjectFactory.Create();
            var provider = new LaunchProfilesProjectPropertiesProvider(project);

            var commonProperties = provider.GetCommonProperties();

            Assert.Null(commonProperties);
        }

        [Fact]
        public void WhenRetrievingItemTypeProperties_NullIsReturned()
        {
            var project = UnconfiguredProjectFactory.Create();
            var provider = new LaunchProfilesProjectPropertiesProvider(project);

            var itemTypeProperties = provider.GetItemTypeProperties(LaunchProfilesProjectItemProvider.ItemType);

            Assert.Null(itemTypeProperties);
        }

        [Fact]
        public void WhenRetrievingItemProperties_NullIsReturnedIfTheItemIsNull()
        {
            var project = UnconfiguredProjectFactory.Create();
            var provider = new LaunchProfilesProjectPropertiesProvider(project);

            var itemProperties = provider.GetItemProperties(LaunchProfilesProjectItemProvider.ItemType, item: null);

            Assert.Null(itemProperties);
        }
    }
}
