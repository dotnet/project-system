// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
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
            var instancePropertiesMock = IProjectPropertiesFactory.MockWithPropertiesAndValues(
                new Dictionary<string, string?>
                {
                    { AssemblyOriginatorKeyFilePropertyName, keyFileFullPath }
                });

            var instanceProperties = instancePropertiesMock.Object;
            var instanceProvider = IProjectInstancePropertiesProviderFactory.ImplementsGetCommonProperties(instanceProperties);

            // Verify get key file value without intercepted provider.
            var properties = instanceProvider.GetCommonProperties(null);
            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(AssemblyOriginatorKeyFilePropertyName);
            Assert.Equal(keyFileFullPath, propertyValue);

            // Verify relative path key file value from intercepted key file provider.
            var project = UnconfiguredProjectFactory.Create(filePath: projectFullPath);
            var delegateProvider = IProjectPropertiesProviderFactory.Create();
            var keyFileProvider = new AssemblyOriginatorKeyFileValueProvider(project);
            var providerMetadata = IInterceptingPropertyValueProviderMetadataFactory.Create(AssemblyOriginatorKeyFilePropertyName);
            var lazyArray = new[] { new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(
                () => keyFileProvider, providerMetadata) };
            var interceptedProvider = new ProjectFileInterceptedViaSnapshotProjectPropertiesProvider(delegateProvider, instanceProvider, project, lazyArray);
            var propertyNames = await properties.GetPropertyNamesAsync();
            Assert.Single(propertyNames);
            Assert.Equal(AssemblyOriginatorKeyFilePropertyName, propertyNames.First());
            properties = interceptedProvider.GetCommonProperties(null);
            string newKeyFileName = "KeyFile2.snk";
            string newKeyFileFullPath = $@"{projectFolder}\{newKeyFileName}";
            await properties.SetPropertyValueAsync(AssemblyOriginatorKeyFilePropertyName, newKeyFileFullPath);
            propertyValue = await properties.GetEvaluatedPropertyValueAsync(AssemblyOriginatorKeyFilePropertyName);
            Assert.Equal(newKeyFileName, propertyValue);
        }
    }
}
