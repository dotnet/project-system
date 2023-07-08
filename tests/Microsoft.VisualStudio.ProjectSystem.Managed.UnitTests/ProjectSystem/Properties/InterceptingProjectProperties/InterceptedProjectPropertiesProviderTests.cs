// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class InterceptedProjectPropertiesProviderTests
    {
        private const string MockPropertyName = "MockProperty";

        [Fact]
        public async Task VerifyInterceptedPropertiesProviderAsync()
        {
            var delegatePropertiesMock = IProjectPropertiesFactory.MockWithPropertiesAndValues(
                new Dictionary<string, string?>
                {
                    { MockPropertyName, "DummyValue" }
                });

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            bool getEvaluatedInvoked = false;
            bool getUnevaluatedInvoked = false;
            bool setValueInvoked = false;

            var mockPropertyProvider = IInterceptingPropertyValueProviderFactory.Create(MockPropertyName,
                onGetEvaluatedPropertyValue: (v, p) => { getEvaluatedInvoked = true; return v; },
                onGetUnevaluatedPropertyValue: (v, p) => { getUnevaluatedInvoked = true; return v; },
                onSetPropertyValue: (v, p, d) => { setValueInvoked = true; return v; });
            var project = UnconfiguredProjectFactory.Create();
            var instanceProvider = IProjectInstancePropertiesProviderFactory.Create();

            var interceptedProvider = new ProjectFileInterceptedProjectPropertiesProvider(delegateProvider, instanceProvider, project, new[] { mockPropertyProvider });
            var properties = interceptedProvider.GetProperties("path/to/project.testproj", null, null);

            // Verify interception for GetEvaluatedPropertyValueAsync.
            string? propertyValue = await properties.GetEvaluatedPropertyValueAsync(MockPropertyName);
            Assert.True(getEvaluatedInvoked);

            // Verify interception for GetUnevaluatedPropertyValueAsync.
            propertyValue = await properties.GetUnevaluatedPropertyValueAsync(MockPropertyName);
            Assert.True(getUnevaluatedInvoked);

            // Verify interception for SetPropertyValueAsync.
            await properties.SetPropertyValueAsync(MockPropertyName, "NewValue", null);
            Assert.True(setValueInvoked);
        }

        [Fact]
        public async Task VerifyInterceptedViaSnapshotCommonPropertiesProviderAsync()
        {
            var delegatePropertiesMock = IProjectPropertiesFactory.MockWithPropertiesAndValues(
                new Dictionary<string, string?>
                {
                    { MockPropertyName, "DummyValue" }
                });

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(commonProps: delegateProperties);

            bool getEvaluatedInvoked = false;
            bool getUnevaluatedInvoked = false;
            bool setValueInvoked = false;

            var mockPropertyProvider = IInterceptingPropertyValueProviderFactory.Create(MockPropertyName,
                onGetEvaluatedPropertyValue: (v, p) => { getEvaluatedInvoked = true; return v; },
                onGetUnevaluatedPropertyValue: (v, p) => { getUnevaluatedInvoked = true; return v; },
                onSetPropertyValue: (v, p, d) => { setValueInvoked = true; return v; });
            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var instanceProvider = IProjectInstancePropertiesProviderFactory.Create();

            var interceptedProvider = new ProjectFileInterceptedViaSnapshotProjectPropertiesProvider(delegateProvider, instanceProvider, unconfiguredProject, new[] { mockPropertyProvider });
            var properties = interceptedProvider.GetCommonProperties();

            // Verify interception for GetEvaluatedPropertyValueAsync.
            string? propertyValue = await properties.GetEvaluatedPropertyValueAsync(MockPropertyName);
            Assert.True(getEvaluatedInvoked);

            // Verify interception for GetUnevaluatedPropertyValueAsync.
            propertyValue = await properties.GetUnevaluatedPropertyValueAsync(MockPropertyName);
            Assert.True(getUnevaluatedInvoked);

            // Verify interception for SetPropertyValueAsync.
            await properties.SetPropertyValueAsync(MockPropertyName, "NewValue", null);
            Assert.True(setValueInvoked);
        }

        [Fact]
        public async Task VerifyInterceptedViaSnapshotInstanceCommonPropertiesProviderAsync()
        {
            var delegatePropertiesMock = IProjectPropertiesFactory.MockWithPropertiesAndValues(
                new Dictionary<string, string?>
                {
                    { MockPropertyName, "DummyValue" }
                });

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateInstanceProvider = IProjectInstancePropertiesProviderFactory.ImplementsGetCommonProperties(delegateProperties);

            bool getEvaluatedInvoked = false;
            bool getUnevaluatedInvoked = false;
            bool setValueInvoked = false;

            var mockPropertyProvider = IInterceptingPropertyValueProviderFactory.Create(MockPropertyName,
                onGetEvaluatedPropertyValue: (v, p) => { getEvaluatedInvoked = true; return v; },
                onGetUnevaluatedPropertyValue: (v, p) => { getUnevaluatedInvoked = true; return v; },
                onSetPropertyValue: (v, p, d) => { setValueInvoked = true; return v; });

            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var provider = IProjectPropertiesProviderFactory.Create();

            var interceptedProvider = new ProjectFileInterceptedViaSnapshotProjectPropertiesProvider(provider, delegateInstanceProvider, unconfiguredProject, new[] { mockPropertyProvider });
            var properties = interceptedProvider.GetCommonProperties(projectInstance: null!);

            // Verify interception for GetEvaluatedPropertyValueAsync.
            string? propertyValue = await properties.GetEvaluatedPropertyValueAsync(MockPropertyName);
            Assert.True(getEvaluatedInvoked);

            // Verify interception for GetUnevaluatedPropertyValueAsync.
            propertyValue = await properties.GetUnevaluatedPropertyValueAsync(MockPropertyName);
            Assert.True(getUnevaluatedInvoked);

            // Verify interception for SetPropertyValueAsync.
            await properties.SetPropertyValueAsync(MockPropertyName, "NewValue", null);
            Assert.True(setValueInvoked);
        }
    }
}
