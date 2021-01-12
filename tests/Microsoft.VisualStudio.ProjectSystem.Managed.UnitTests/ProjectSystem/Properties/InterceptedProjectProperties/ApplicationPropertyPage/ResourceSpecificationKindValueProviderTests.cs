// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class ResourceSpecificationKindValueProviderTests
    {
        [Fact]
        public async Task WhenSettingTheValue_TheNewValueIsStoredInTemporaryStorage()
        {
            var storageDictionary = new Dictionary<string, string>();
            var provider = new ResourceSpecificationKindValueProvider(ITemporaryPropertyStorageFactory.Create(storageDictionary));

            var result = await provider.OnSetPropertyValueAsync(
                ResourceSpecificationKindValueProvider.ResourceSpecificationKindProperty,
                ResourceSpecificationKindValueProvider.ResourceFileValue,
                Mock.Of<IProjectProperties>());

            Assert.Null(result);
            Assert.True(storageDictionary.TryGetValue(ResourceSpecificationKindValueProvider.ResourceSpecificationKindProperty, out string savedValue));
            Assert.Equal(expected: ResourceSpecificationKindValueProvider.ResourceFileValue, actual: savedValue);
        }

        [Fact]
        public async Task WhenGettingTheValue_DefaultsToIconAndManifest()
        {
            var provider = new ResourceSpecificationKindValueProvider(ITemporaryPropertyStorageFactory.Create());

            var result = await provider.OnGetEvaluatedPropertyValueAsync(
                ResourceSpecificationKindValueProvider.ResourceSpecificationKindProperty,
                string.Empty,
                Mock.Of<IProjectProperties>());

            Assert.Equal(expected: ResourceSpecificationKindValueProvider.IconAndManifestValue, actual: result);
        }

        [Fact]
        public async Task WhenGettingTheValue_TheValueIsRetrievedFromTemporaryStorageIfAvailable()
        {
            var storageDictionary = new Dictionary<string, string> { [ResourceSpecificationKindValueProvider.ResourceSpecificationKindProperty] = ResourceSpecificationKindValueProvider.ResourceFileValue };
            var provider = new ResourceSpecificationKindValueProvider(ITemporaryPropertyStorageFactory.Create(storageDictionary));

            var result = await provider.OnGetEvaluatedPropertyValueAsync(
                ResourceSpecificationKindValueProvider.ResourceSpecificationKindProperty,
                string.Empty,
                Mock.Of<IProjectProperties>());

            Assert.Equal(expected: ResourceSpecificationKindValueProvider.ResourceFileValue, actual: result);
        }

        [Fact]
        public async Task WhenGettingTheValue_ReturnsResourceFileIfWin32ResourceIsSet()
        {
            var provider = new ResourceSpecificationKindValueProvider(ITemporaryPropertyStorageFactory.Create());
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue(ResourceSpecificationKindValueProvider.Win32ResourceMSBuildProperty, @"C:\alpha\beta\gamma.res");

            var result = await provider.OnGetEvaluatedPropertyValueAsync(
                ResourceSpecificationKindValueProvider.ResourceSpecificationKindProperty,
                string.Empty,
                defaultProperties);

            Assert.Equal(expected: ResourceSpecificationKindValueProvider.ResourceFileValue, actual: result);
        }

        [Fact]
        public async Task WhenGettingTheValue_ReturnsIconAndManifestIfIconSet()
        {
            var provider = new ResourceSpecificationKindValueProvider(ITemporaryPropertyStorageFactory.Create());
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue(ResourceSpecificationKindValueProvider.ApplicationIconMSBuildProperty, @"C:\alpha\beta\gamma.ico");

            var result = await provider.OnGetEvaluatedPropertyValueAsync(
                ResourceSpecificationKindValueProvider.ResourceSpecificationKindProperty,
                string.Empty,
                defaultProperties);

            Assert.Equal(expected: ResourceSpecificationKindValueProvider.IconAndManifestValue, actual: result);
        }

        [Fact]
        public async Task WhenGettingTheValue_ReturnsIconAndManifestIfManifestSet()
        {
            var provider = new ResourceSpecificationKindValueProvider(ITemporaryPropertyStorageFactory.Create());
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue(ResourceSpecificationKindValueProvider.ApplicationManifestMSBuildProperty, @"C:\alpha\beta\app.config");

            var result = await provider.OnGetEvaluatedPropertyValueAsync(
                ResourceSpecificationKindValueProvider.ResourceSpecificationKindProperty,
                string.Empty,
                defaultProperties);

            Assert.Equal(expected: ResourceSpecificationKindValueProvider.IconAndManifestValue, actual: result);
        }
    }
}
