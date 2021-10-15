// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties.Package;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.InterceptedProjectProperties.PackagePropertyPage
{
    public class PackageFilePropertyValueProviderBaseTests
    {
        private const string TestPropertyName = "Caketown";

        [Theory]
        //          Expression  File                Stored value   Expected value
        [InlineData(null, null, null, "None")]
        [InlineData(null, null, "File", "File")]
        [InlineData(null, @"C:\license.txt", null, "File")]
        [InlineData(null, @"C:\license.txt", "Expression", "File")]
        [InlineData("alpha", null, null, "Expression")]
        [InlineData("alpha", @"C:\license.txt", null, "Expression")]
        [InlineData("alpha", @"C:\license.txt", "None", "Expression")]
        //public async Task GetPackageLicenseKind(string? expressionPropertyValue, string? filePropertyValue, string? storedValue, string expectedValue)
        public async Task GetPackageLicenseKind(string include, string packagePath, string expectedValue)
        {
            //var projectTree = IProjectTreeProviderFactory.Create();
            var projectItemProvider = IProjectItemProviderFactory.Create();
            //await projectItemProvider.AddItemAsync(None.SchemaName, include, METADATA);
            //await projectItemProvider.AddItemAsync((it, i, md) => null);
            await projectItemProvider.AddAsync(None.SchemaName, include, new Dictionary<string, string>
                {
                    { "Pack", bool.TrueString },
                    { "PackagePath", packagePath }
                });

            var unconfiguredProject = UnconfiguredProjectFactory.Create();
            var provider = new TestValueProvider(TestPropertyName, projectItemProvider, unconfiguredProject);
            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, null!);

            Assert.Equal(expected: expectedValue, actual: actualValue);
        }

        [Theory]

        //          New value      Current     Current             Expected    Expected            Expected
        //                         Expression  File                Expression  File                stored value
        [InlineData("Expression", null, null, null, null, "Expression")]
        [InlineData("Expression", "alpha", null, "alpha", null, "Expression")]
        [InlineData("Expression", null, @"C:\license.txt", null, null, "Expression")]
        [InlineData("File", null, null, null, null, "File")]
        [InlineData("File", "alpha", @"C:\license.txt", null, @"C:\license.txt", "File")]
        [InlineData("File", "alpha", null, null, null, "File")]
        [InlineData("None", "alpha", null, null, null, "None")]
        [InlineData("None", null, @"C:\license.txt", null, null, "None")]
        public async Task SetPackageLicenseKind(string newValue, string? currentExpressionPropertyValue, string? currentFilePropertyValue, string? expectedExpressionPropertyValue, string? expectedFilePropertyValue, string? expectedStoredValue)
        {
            Dictionary<string, string> storageDictionary = new();
            var storage = ITemporaryPropertyStorageFactory.Create(storageDictionary);

            Dictionary<string, string?> defaultPropertiesDictionary = new();
            defaultPropertiesDictionary["PackageLicenseExpression"] = currentExpressionPropertyValue;
            defaultPropertiesDictionary["PackageLicenseFile"] = currentFilePropertyValue;
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(defaultPropertiesDictionary);

            var provider = new TestValueProvider(storage);
            await provider.OnSetPropertyValueAsync("", newValue, defaultProperties);

            defaultPropertiesDictionary.TryGetValue("PackageLicenseExpression", out string? finalExpressionPropertyValue);
            defaultPropertiesDictionary.TryGetValue("PackageLicenseFile", out string? finalFilePropertyValue);
            storageDictionary.TryGetValue("PackageLicenseKind", out string? finalStoredValue);

            Assert.Equal(expectedExpressionPropertyValue, finalExpressionPropertyValue);
            Assert.Equal(expectedFilePropertyValue, finalFilePropertyValue);
            Assert.Equal(expectedStoredValue, finalStoredValue);
        }

        private class TestValueProvider : PackageFilePropertyValueProviderBase
        {
            [ImportingConstructor]
            public TestValueProvider(
                string propertyName,
                [Import(ExportContractNames.ProjectItemProviders.SourceFiles)] IProjectItemProvider sourceItemsProvider,
                UnconfiguredProject unconfiguredProject) :
                base(propertyName, sourceItemsProvider, unconfiguredProject)
            {
            }
        }
    }
}
