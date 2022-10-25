// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.Versioning;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    public class TargetFrameworkValueProviderTests
    {
        private const string TargetFrameworkPropertyName = "TargetFramework";

        private static InterceptedProjectPropertiesProviderBase CreateInstance(FrameworkName configuredTargetFramework)
        {
            var data = new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkMonikerProperty, configuredTargetFramework.FullName);

            var project = UnconfiguredProjectFactory.Create();
            var properties = ProjectPropertiesFactory.Create(project, data);
            var delegateProvider = IProjectPropertiesProviderFactory.Create();

            var instancePropertiesMock = IProjectPropertiesFactory
                .MockWithProperty(TargetFrameworkPropertyName);

            var instanceProperties = instancePropertiesMock.Object;
            var instanceProvider = IProjectInstancePropertiesProviderFactory.ImplementsGetCommonProperties(instanceProperties);

            var targetFrameworkProvider = new TargetFrameworkValueProvider(properties);
            var providerMetadata = IInterceptingPropertyValueProviderMetadataFactory.Create(TargetFrameworkPropertyName);
            var lazyArray = new[] { new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(
                () => targetFrameworkProvider, providerMetadata) };
            return new ProjectFileInterceptedViaSnapshotProjectPropertiesProvider(delegateProvider, instanceProvider, project, lazyArray);
        }

        [Fact]
        public async Task VerifyGetTargetFrameworkPropertyAsync()
        {
            var configuredTargetFramework = new FrameworkName(".NETFramework", new Version(4, 5));
            var expectedTargetFrameworkPropertyValue = (uint)0x40005;
            var provider = CreateInstance(configuredTargetFramework);
            var properties = provider.GetCommonProperties(null!);
            var propertyValueStr = await properties.GetEvaluatedPropertyValueAsync(TargetFrameworkPropertyName);
            Assert.True(uint.TryParse(propertyValueStr, out uint propertyValue));
            Assert.Equal(expectedTargetFrameworkPropertyValue, propertyValue);
        }
    }
}
