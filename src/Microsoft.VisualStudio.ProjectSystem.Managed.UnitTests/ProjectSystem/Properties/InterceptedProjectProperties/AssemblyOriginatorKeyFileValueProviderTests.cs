// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    [ProjectSystemTrait]
    public class AssemblyOriginatorKeyFileValueProviderTests
    {
        private const string AssemblyOriginatorKeyFilePropertyName = "AssemblyOriginatorKeyFile";

        [Fact]
        public async Task VerifySetKeyFilePropertyAsync()
        {
            string projectFolder = @"C:\project\root";
            string projectFullPath = $@"{projectFolder}\project.testproj";
            string keyFileName = "KeyFile.snk";
            string keyFileFullPath = $@"{projectFolder}\{keyFileName}";
            var delegatePropertiesMock = IProjectPropertiesFactory
                .MockWithPropertiesAndValues(new Dictionary<string, string>() {
                    { AssemblyOriginatorKeyFilePropertyName, keyFileFullPath }
                });

            var delegateProperties = delegatePropertiesMock.Object;
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            // Verify get key file value without intercepted provider.
            var properties = delegateProvider.GetProperties("path/to/project.testproj", null, null);
            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(AssemblyOriginatorKeyFilePropertyName);
            Assert.Equal(keyFileFullPath, propertyValue);

            // Verify relative path key file value from intercepted key file provider.
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(filePath: projectFullPath);
            var keyFileprovider = new AssemblyOriginatorKeyFileValueProvider(unconfiguredProject);
            var providerMetadata = IInterceptingPropertyValueProviderMetadataFactory.Create(AssemblyOriginatorKeyFilePropertyName);
            var interceptedProvider = new InterceptedProjectPropertiesProvider(delegateProvider, unconfiguredProject, keyFileprovider, providerMetadata);
            var propertyNames = await properties.GetPropertyNamesAsync();
            Assert.Equal(1, propertyNames.Count());
            Assert.Equal(AssemblyOriginatorKeyFilePropertyName, propertyNames.First());
            properties = interceptedProvider.GetProperties("path/to/project.testproj", null, null);
            string newKeyFileName = "KeyFile2.snk";
            string newKeyFileFullPath = $@"{projectFolder}\{newKeyFileName}";
            await properties.SetPropertyValueAsync(AssemblyOriginatorKeyFilePropertyName, newKeyFileFullPath);
            propertyValue = await properties.GetEvaluatedPropertyValueAsync(AssemblyOriginatorKeyFilePropertyName);
            Assert.Equal(newKeyFileName, propertyValue);
        }
    }
}