using Microsoft.VisualStudio.ProjectSystem.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    [ProjectSystemTrait]
    public class AssemblyOriginatorKeyFileValueProviderTests
    {
        private const string AssemblyOriginatorKeyFilePropertyName = "AssemblyOriginatorKeyFile";

        [Fact]
        public void VerifySetKeyFileProperty()
        {
            string projectFolder = @"C:\project\root";
            string projectFullPath = $@"{projectFolder}\project.testproj";
            string keyFileName = "KeyFile.snk";
            string keyFileFullPath = $@"{projectFolder}\{keyFileName}";
            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithPropertiesAndGetSet(new Dictionary<string, string>() {
                    { AssemblyOriginatorKeyFilePropertyName, keyFileFullPath }
                });

            delegatePropertiesMock.SetupGet(t => t.FileFullPath)
                .Returns(projectFullPath);

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            // Verify get key file value without intercepted provider.
            var properties = delegateProvider.GetProperties("path/to/project.testproj", null, null);
            var propertyValue = properties.GetEvaluatedPropertyValueAsync(AssemblyOriginatorKeyFilePropertyName).Result;
            Assert.Equal(keyFileFullPath, propertyValue);

            // Verify relative path key file value from intercepted key file provider.
            var keyFileprovider = new AssemblyOriginatorKeyFileValueProvider();
            var interceptedProvider = new InterceptedProjectPropertiesProvider(delegateProvider, keyFileprovider);
            var propertyNames = properties.GetPropertyNamesAsync().Result;
            Assert.Equal(1, propertyNames.Count());
            Assert.Equal(AssemblyOriginatorKeyFilePropertyName, propertyNames.First());

            properties = interceptedProvider.GetProperties("path/to/project.testproj", null, null);
            string newKeyFileName = "KeyFile2.snk";
            string newKeyFileFullPath = $@"{projectFolder}\{newKeyFileName}";
            properties.SetPropertyValueAsync(AssemblyOriginatorKeyFilePropertyName, newKeyFileFullPath).Wait();
            propertyValue = properties.GetEvaluatedPropertyValueAsync(AssemblyOriginatorKeyFilePropertyName).Result;
            Assert.Equal(newKeyFileName, propertyValue);
        }
    }
}