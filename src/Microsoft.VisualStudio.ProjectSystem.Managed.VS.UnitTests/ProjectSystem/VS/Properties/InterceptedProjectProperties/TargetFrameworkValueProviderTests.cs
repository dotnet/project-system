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

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    [ProjectSystemTrait]
    public class TargetFrameworkValueProviderTests
    {
        private const string TargetFrameworkPropertyName = "TargetFramework";

        private static IProjectProperties GetMockInterceptedProperties(bool configureHierarchy, uint? configuredTargetFramework)
        {
            IUnconfiguredProjectVsServices projectVsServices;
            if (configureHierarchy)
            {
                Assert.NotNull(configuredTargetFramework);
                var hierarchy = IVsHierarchyFactory.Create();
                hierarchy.ImplementGetProperty(Shell.VsHierarchyPropID.TargetFrameworkVersion, result: (uint)0x50000);
                projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);
            }
            else
            {
                Assert.Null(configuredTargetFramework);
                projectVsServices = IUnconfiguredProjectVsServicesFactory.Create();
            }

            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithProperty(TargetFrameworkPropertyName);

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            var targetFrameworkProvider = new TargetFrameworkValueProvider(projectVsServices);
            var interceptedProvider = new InterceptedProjectPropertiesProvider(delegateProvider, targetFrameworkProvider);
            return interceptedProvider.GetProperties("path/to/project.testproj", null, null);
        }

        [Fact]
        public void CreateTargetProvider_WithNullProjectVsServices_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new TargetFrameworkValueProvider(projectVsServices: null);
            });
        }

        [Fact]
        public async Task CreateTargetProvider_WithInvalidHierarchy_Throws()
        {
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Create();
            var properties = GetMockInterceptedProperties(configureHierarchy: false, configuredTargetFramework: null);
            await Assert.ThrowsAsync<ArgumentNullException>("vsHierarchy", async () =>
            {
                await properties.GetEvaluatedPropertyValueAsync(TargetFrameworkPropertyName);
            });
        }

        [Fact]
        public void VerifyGetTargetFrameworkProperty()
        {
            var configuredTargetFramework = (uint)0x50000;
            var properties = GetMockInterceptedProperties(configureHierarchy: true, configuredTargetFramework: configuredTargetFramework);
            var propertyValueStr = properties.GetEvaluatedPropertyValueAsync(TargetFrameworkPropertyName).Result;
            uint propertyValue;
            Assert.True(uint.TryParse(propertyValueStr, out propertyValue));
            Assert.Equal(configuredTargetFramework, propertyValue);
        }
    }
}