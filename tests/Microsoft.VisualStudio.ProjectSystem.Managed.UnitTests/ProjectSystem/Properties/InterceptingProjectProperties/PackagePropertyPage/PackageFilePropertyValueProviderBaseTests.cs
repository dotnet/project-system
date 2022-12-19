// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties.Package;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.InterceptingProjectProperties.PackagePropertyPage
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
        public async Task GetPackageFilePropertyValue(string? existingInclude, string? pack, string? propertyValue, string? expectedValue)
        {
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
        [InlineData(null, null, null, null, "", "")]
        [InlineData(null, null, null, null, @"Test.txt", @"Test.txt")]
        [InlineData(null, null, "True", "", @"Test.txt", @"Test.txt")]
        [InlineData(null, null, "True", "", "", "")]
        [InlineData(null, null, "True", "docs", "", "")]
        [InlineData(@"..\..\..\Test.txt", null, "True", null, @"Test.txt", @"Test.txt")]
        [InlineData(@"..\..\..\TotallyDifferentFile.txt", null, "True", null, @"Test.txt", @"Test.txt")]
        [InlineData(@"..\..\..\Test.txt", @"docs\Test.txt", "False", "docs", @"InAFolder\Test.txt", @"Test.txt")]
        [InlineData(@"..\..\..\Test.txt", @"docs\Test.txt", "True", "docs", @"InAFolder\Test.txt", @"docs\Test.txt")]
        [InlineData(@"..\..\..\Test.txt", @"docs\Test.txt", null, null, "", "")]
        [InlineData(@"TotallyDifferentFile.txt", @"docs\TotallyDifferentFile.txt", "True", "docs", @"C:\Test.txt", @"docs\Test.txt")]
        [InlineData(@"..\..\..\Test.txt", @"docs\Test.txt", "True", "docs", ProjectPath + @"\NewOne.txt", @"docs\NewOne.txt")]
        [InlineData(@"Documents\Test.txt", @"docs\Test.txt", "True", "docs", ProjectPath + @"\TotallyDifferentFile.txt", @"docs\TotallyDifferentFile.txt")]
        [InlineData(@"Test.txt", @"Test.txt", "True", "", @"Test.txt", @"Test.txt")]
        [InlineData(ProjectPath + @"\Test.txt", @"docs\Test.txt", "True", "", "NewOne.txt", "NewOne.txt")]
        [InlineData(ProjectPath + @"\Test.txt", @"docs\Test.txt", "True", @"in\this\folder", "NewOne.txt", @"in\this\folder\NewOne.txt")]
        [InlineData("", "", "True", "", @"Test.txt", "Test.txt")]
        [InlineData("", "", "True", "docs", @"Test.txt", @"docs\Test.txt")]
        public async Task SetPackageFilePropertyValue(string? existingInclude, string? existingPropertyValue, string? pack, string? packagePath, string newPropertyValue, string expectedValue)
        {
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
            var actualValue = await provider.OnSetPropertyValueAsync(string.Empty, newPropertyValue, existingProperties);
            Assert.Equal(expected: expectedValue, actual: actualValue);
        }

        private class TestValueProvider : PackageFilePropertyValueProviderBase
        {
            public TestValueProvider(string propertyName, IProjectItemProvider sourceItemsProvider, UnconfiguredProject unconfiguredProject)
                : base(propertyName, sourceItemsProvider, unconfiguredProject)
            {
            }
        }
    }
}
