// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties.Package;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.InterceptedProjectProperties.PackagePropertyPage
{
    public class PackageFilePropertyValueProviderBaseTests
    {
        private const string TestPropertyName = "Example";
        private const string ProjectPath = @"C:\Test\Path\Here";
        private static readonly IProjectProperties EmptyProjectProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(ImmutableDictionary<string, string?>.Empty);

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData(null, null, @"Test.txt", @"Test.txt")]
        [InlineData(@"..\..\..\Test.txt", "True", @"Test.txt", @"..\..\..\Test.txt")]
        [InlineData(@"..\..\..\Test.txt", "False", @"docs\Test.txt", @"docs\Test.txt")]
        [InlineData(@"..\..\..\Test.txt", null, @"docs\Test.txt", @"docs\Test.txt")]
        [InlineData(@"TotallyDifferentFile.txt", "True", @"docs\Test.txt", @"docs\Test.txt")]
        [InlineData(@"..\..\..\Test.txt", "True", @"docs\Test.txt", @"..\..\..\Test.txt")]
        [InlineData(@"Documents\Test.txt", "True", @"docs\Test.txt", @"Documents\Test.txt")]
        [InlineData(@"Test.txt", "True", @"Test.txt", @"Test.txt")]
        [InlineData(ProjectPath + @"\Test.txt", "True", @"Test.txt", ProjectPath + @"\Test.txt")]
        [InlineData(@"C:\Test.txt", "True", @"Test.txt", @"C:\Test.txt")]
        //public async Task GetPackageLicenseKind(string? expressionPropertyValue, string? filePropertyValue, string? storedValue, string expectedValue)
        public async Task GetPackageFilePropertyValue(string? existingInclude, string? pack, string? propertyValue, string? expectedValue)
        {
            //var projectTree = IProjectTreeProviderFactory.Create();
            //var projectItemProvider = IProjectItemProviderFactory.Create();
            //IProjectItemProvider
            //await projectItemProvider.AddItemAsync(None.SchemaName, include, METADATA);
            //await projectItemProvider.AddItemAsync((it, i, md) => null);
            //var metadata = (pack is not null && packagePath is not null) ? new Dictionary<string, string>
            //    {
            //        { "Pack", pack },
            //        { "PackagePath", packagePath }
            //    } : null;
            //await projectItemProvider.AddAsync(None.SchemaName, include, metadata);
            var projectItemProvider = IProjectItemProviderFactory.GetItemsAsync(() => existingInclude is not null ? new[]
            {
                IProjectItemFactory.Create(existingInclude, pack is not null
                    ? IProjectPropertiesFactory.CreateWithPropertyAndValue("Pack", pack)
                    : EmptyProjectProperties)
            } : Enumerable.Empty<IProjectItem>());
            var unconfiguredProject = UnconfiguredProjectFactory.Create(fullPath: ProjectPath);
            var provider = new TestValueProvider(TestPropertyName, projectItemProvider, unconfiguredProject);

            var unevaluatedActualValue = await provider.OnGetUnevaluatedPropertyValueAsync(string.Empty, propertyValue!, null!);
            Assert.Equal(expected: expectedValue, actual: unevaluatedActualValue);

            var evaluatedActualValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, propertyValue!, null!);
            Assert.Equal(expected: expectedValue, actual: evaluatedActualValue);
        }

        [Theory]
        [InlineData("Expression", null, null, null, null, "Expression")]
        [InlineData("Expression", "alpha", null, "alpha", null, "Expression")]
        [InlineData("Expression", null, @"C:\license.txt", null, null, "Expression")]
        [InlineData("File", null, null, null, null, "File")]
        [InlineData("File", "alpha", @"C:\license.txt", null, @"C:\license.txt", "File")]
        [InlineData("File", "alpha", null, null, null, "File")]
        [InlineData("None", "alpha", null, null, null, "None")]
        [InlineData("None", null, @"C:\license.txt", null, null, "None")]
        public async Task SetPackageFilePropertyValue(string? existingInclude, string? existingPropertyValue, string? pack, string? packagePath, string? newPropertyValue, string expectedValue)
        {
            //Dictionary<string, string> storageDictionary = new();
            //var storage = ITemporaryPropertyStorageFactory.Create(storageDictionary);

            //Dictionary<string, string?> defaultPropertiesDictionary = new();
            //defaultPropertiesDictionary["PackageLicenseExpression"] = currentExpressionPropertyValue;
            //defaultPropertiesDictionary["PackageLicenseFile"] = currentFilePropertyValue;
            //var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(defaultPropertiesDictionary);

            //var provider = new TestValueProvider(storage);
            //await provider.OnSetPropertyValueAsync("", newValue, defaultProperties);

            //defaultPropertiesDictionary.TryGetValue("PackageLicenseExpression", out string? finalExpressionPropertyValue);
            //defaultPropertiesDictionary.TryGetValue("PackageLicenseFile", out string? finalFilePropertyValue);
            //storageDictionary.TryGetValue("PackageLicenseKind", out string? finalStoredValue);

            //Assert.Equal(expectedExpressionPropertyValue, finalExpressionPropertyValue);
            //Assert.Equal(expectedFilePropertyValue, finalFilePropertyValue);
            //Assert.Equal(expectedStoredValue, finalStoredValue);

            var existingMetadata = new Dictionary<string, string?>();
            if(pack is not null)
            {
                existingMetadata.Add("Pack", pack);
            }
            if (packagePath is not null)
            {
                existingMetadata.Add("PackagePath", packagePath);
            }
            var projectItemProvider = IProjectItemProviderFactory.GetItemsAsync(() => existingInclude is not null ? new[]
            {
                IProjectItemFactory.Create(existingInclude, IProjectPropertiesFactory.CreateWithPropertiesAndValues(existingMetadata))
            } : Enumerable.Empty<IProjectItem>());
            var unconfiguredProject = UnconfiguredProjectFactory.Create(fullPath: ProjectPath);
            var provider = new TestValueProvider(TestPropertyName, projectItemProvider, unconfiguredProject);

            var existingProperties = existingPropertyValue is not null
                ? IProjectPropertiesFactory.CreateWithPropertyAndValue(TestPropertyName, existingPropertyValue)
                : EmptyProjectProperties;
            var actualValue = await provider.OnSetPropertyValueAsync(string.Empty, newPropertyValue!, existingProperties);
            Assert.Equal(expected: expectedValue, actual: actualValue);
        }

        private class TestValueProvider : PackageFilePropertyValueProviderBase
        {
            public TestValueProvider(string propertyName, IProjectItemProvider sourceItemsProvider, UnconfiguredProject unconfiguredProject) :
                base(propertyName, sourceItemsProvider, unconfiguredProject)
            {
            }
        }
    }
}
