// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
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
        public async Task WhenRetrievingCommonProperties_ThereAreNoPropertyNames()
        {
            var project = UnconfiguredProjectFactory.Create();
            var provider = new LaunchProfilesProjectPropertiesProvider(project);

            var commonProperties = provider.GetCommonProperties();

            var directPropertyNames = await commonProperties.GetDirectPropertyNamesAsync();
            Assert.Empty(directPropertyNames);

            var propertyNames = await commonProperties.GetPropertyNamesAsync();
            Assert.Empty(propertyNames);
        }

        [Fact]
        public void WhenRetrievingCommonProperties_FileFullPathIsTheProjectPath()
        {
            string projectPath = @"C:\alpha\beta\gamma.csproj";
            var project = UnconfiguredProjectFactory.ImplementFullPath(projectPath);

            var provider = new LaunchProfilesProjectPropertiesProvider(project);
            var commonProperties = provider.GetCommonProperties();

            var fileFullPath = commonProperties.FileFullPath;
            Assert.Equal(expected: projectPath, actual: fileFullPath);
        }

        [Fact]
        public void WhenRetrievingCommonProperties_PropertyKindIsPropertyGroup()
        {
            var project = UnconfiguredProjectFactory.Create();

            var provider = new LaunchProfilesProjectPropertiesProvider(project);
            var commonProperties = provider.GetCommonProperties();

            var propertyKind = commonProperties.PropertyKind;
            Assert.Equal(expected: PropertyKind.PropertyGroup, actual: propertyKind);
        }

        [Fact]
        public void WhenRetrievingCommonProperties_TheContextRefersToTheProject()
        {
            string projectPath = @"C:\alpha\beta\gamma.csproj";
            var project = UnconfiguredProjectFactory.ImplementFullPath(projectPath);

            var provider = new LaunchProfilesProjectPropertiesProvider(project);
            var commonProperties = provider.GetCommonProperties();
            var context = commonProperties.Context;

            Assert.Equal(expected: projectPath, actual: context.File);
            Assert.True(context.IsProjectFile);
            Assert.Null(context.ItemName);
            Assert.Null(context.ItemType);
        }

        [Fact]
        public async Task WhenDeletingDirectPropertiesOfCommonProperties_ANotSupportedExceptionIsThrown()
        {
            var project = UnconfiguredProjectFactory.Create();

            var provider = new LaunchProfilesProjectPropertiesProvider(project);
            var commonProperties = provider.GetCommonProperties();

            await Assert.ThrowsAsync<NotSupportedException>(() => commonProperties.DeleteDirectPropertiesAsync());
        }

        [Fact]
        public async Task WhenDeletingAPropertyOfTheCommonProperties_ANotSupportedExceptionIsThrown()
        {
            var project = UnconfiguredProjectFactory.Create();

            var provider = new LaunchProfilesProjectPropertiesProvider(project);
            var commonProperties = provider.GetCommonProperties();

            await Assert.ThrowsAsync<NotSupportedException>(() => commonProperties.DeletePropertyAsync("Alpha"));
        }

        [Fact]
        public async Task WhenGettingTheValueOfACommonProperty_ANotSupportedExceptionIsThrown()
        {
            var project = UnconfiguredProjectFactory.Create();

            var provider = new LaunchProfilesProjectPropertiesProvider(project);
            var commonProperties = provider.GetCommonProperties();

            await Assert.ThrowsAsync<NotSupportedException>(() => commonProperties.GetEvaluatedPropertyValueAsync("Alpha"));
            await Assert.ThrowsAsync<NotSupportedException>(() => commonProperties.GetUnevaluatedPropertyValueAsync("Alpha"));
        }

        [Fact]
        public async Task WhenCheckingIfACommonPropertyIsInherited_TheResultIsFalse()
        {
            var project = UnconfiguredProjectFactory.Create();

            var provider = new LaunchProfilesProjectPropertiesProvider(project);
            var commonProperties = provider.GetCommonProperties();

            Assert.False(await commonProperties.IsValueInheritedAsync("Alpha"));
        }

        [Fact]
        public async Task WhenSettingTheValueOfACommonProperty_ANotSupportedExceptionIsThrown()
        {
            var project = UnconfiguredProjectFactory.Create();

            var provider = new LaunchProfilesProjectPropertiesProvider(project);
            var commonProperties = provider.GetCommonProperties();

            await Assert.ThrowsAsync<NotSupportedException>(() => commonProperties.SetPropertyValueAsync("Alpha", "Value"));
        }

        [Fact]
        public void WhenRequestingPropertiesForTheProjectFile_TheCommonPropertiesAreReturned()
        {
            string projectPath = @"C:\alpha\beta\gamma.csproj";
            var project = UnconfiguredProjectFactory.ImplementFullPath(projectPath);

            var provider = new LaunchProfilesProjectPropertiesProvider(project);
            var properties = provider.GetProperties(file: projectPath, itemType: null, item: null);
            var context = properties.Context;

            Assert.Equal(expected: projectPath, actual: context.File);
            Assert.True(context.IsProjectFile);
            Assert.Null(context.ItemName);
            Assert.Null(context.ItemType);
        }
    }
}
