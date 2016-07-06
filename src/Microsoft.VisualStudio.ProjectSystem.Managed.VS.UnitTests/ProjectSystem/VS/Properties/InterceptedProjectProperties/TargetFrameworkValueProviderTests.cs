using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Properties;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Runtime.Versioning;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    [ProjectSystemTrait]
    public class TargetFrameworkValueProviderTests
    {
        private const string TargetFrameworkPropertyName = "TargetFramework";

        private InterceptedProjectPropertiesProvider CreateInstance(FrameworkName configuredTargetFramework)
        {
            var data = new PropertyPageData()
            {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.TargetFrameworkMonikerProperty,
                Value = configuredTargetFramework.FullName
            };

            var project = IUnconfiguredProjectFactory.Create();
            var properties = ProjectPropertiesFactory.Create(project, data);

            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithProperty(TargetFrameworkPropertyName);

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            var targetFrameworkProvider = new TargetFrameworkValueProvider(properties);
            var providerMetadata = IInterceptingPropertyValueProviderMetadataFactory.Create(TargetFrameworkPropertyName);
            return new InterceptedProjectPropertiesProvider(delegateProvider, targetFrameworkProvider, providerMetadata);
        }

        [Fact]
        public void CreateTargetProvider_WithNullProjectVsServices_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new TargetFrameworkValueProvider(properties: null);
            });
        }

        [Fact]
        public async Task VerifyGetTargetFrameworkPropertyAsync()
        {
            var configuredTargetFramework = new FrameworkName(".NETFramework", new Version(4, 5));
            var expectedTargetFrameworkPropertyValue = (uint)0x40005;
            var provider = CreateInstance(configuredTargetFramework);
            var properties = provider.GetProperties("path/to/project.testproj", null, null);
            var propertyValueStr = await properties.GetEvaluatedPropertyValueAsync(TargetFrameworkPropertyName);
            uint propertyValue;
            Assert.True(uint.TryParse(propertyValueStr, out propertyValue));
            Assert.Equal(expectedTargetFrameworkPropertyValue, propertyValue);
        }
    }
}