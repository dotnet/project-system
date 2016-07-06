using Microsoft.VisualStudio.ProjectSystem.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    [ProjectSystemTrait]
    public class InterceptedProjectPropertiesProviderTests
    {
        private class MockInterceptedProjectPropertyProvider : InterceptingPropertyValueProviderBase
        {
            public const string PropertyName = "MockProperty";

            public bool GetEvaluatedInvoked;
            public bool GetUnevaluatedInvoked;
            public bool SetValueInvoked;

            public override string GetPropertyName() => PropertyName;

            public override Task<string> InterceptGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
            {
                GetEvaluatedInvoked = true;
                return base.InterceptGetEvaluatedPropertyValueAsync(evaluatedPropertyValue, defaultProperties);
            }

            public override Task<string> InterceptGetUnevaluatedPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties)
            {
                GetUnevaluatedInvoked = true;
                return base.InterceptGetUnevaluatedPropertyValueAsync(unevaluatedPropertyValue, defaultProperties);
            }

            public override Task<string> InterceptSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
            {
                SetValueInvoked = true;
                return base.InterceptSetPropertyValueAsync(unevaluatedPropertyValue, defaultProperties, dimensionalConditions);
            }
        }

        [Fact]
        public void VerifyInterceptedPropertiesProvider()
        {
            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithPropertiesAndGetSet(new Dictionary<string, string>() {
                    { MockInterceptedProjectPropertyProvider.PropertyName, "DummyValue" }
                });
            
            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);
            var mockPropertyProvider = new MockInterceptedProjectPropertyProvider();
            var interceptedProvider = new InterceptedProjectPropertiesProvider(delegateProvider, mockPropertyProvider);
            var properties = interceptedProvider.GetProperties("path/to/project.testproj", null, null);

            // Verify defaults
            Assert.False(mockPropertyProvider.GetEvaluatedInvoked);
            Assert.False(mockPropertyProvider.GetUnevaluatedInvoked);
            Assert.False(mockPropertyProvider.SetValueInvoked);

            // Verify interception for GetEvaluatedPropertyValueAsync.
            var propertyValue = properties.GetEvaluatedPropertyValueAsync(MockInterceptedProjectPropertyProvider.PropertyName).Result;
            Assert.True(mockPropertyProvider.GetEvaluatedInvoked);

            // Verify interception for GetUnevaluatedPropertyValueAsync.
            propertyValue = properties.GetUnevaluatedPropertyValueAsync(MockInterceptedProjectPropertyProvider.PropertyName).Result;
            Assert.True(mockPropertyProvider.GetUnevaluatedInvoked);

            // Verify interception for SetPropertyValueAsync.
            properties.SetPropertyValueAsync(MockInterceptedProjectPropertyProvider.PropertyName, "NewValue", null);
            Assert.True(mockPropertyProvider.SetValueInvoked);
        }
    }
}