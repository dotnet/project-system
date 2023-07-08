// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class PackageLicenseKindValueProviderTests
    {
        [Theory]
        //          Expression  File                Stored value   Expected value
        [InlineData(null,       null,               null,         "None")]
        [InlineData(null,       null,               "File",       "File")]
        [InlineData(null,       @"C:\license.txt",  null,         "File")]
        [InlineData(null,       @"C:\license.txt",  "Expression", "File")]
        [InlineData("alpha",    null,               null,         "Expression")]
        [InlineData("alpha",    @"C:\license.txt",  null,         "Expression")]
        [InlineData("alpha",    @"C:\license.txt",  "None",       "Expression")]
        public async Task GetPackageLicenseKind(string? expressionPropertyValue, string? filePropertyValue, string? storedValue, string expectedValue)
        {
            Dictionary<string, string>? storedValues = null;
            if (storedValue is not null)
            {
                storedValues = new Dictionary<string, string>
                {
                    { "PackageLicenseKind", storedValue }
                };
            }
            var storage = ITemporaryPropertyStorageFactory.Create(storedValues);
            var provider = new PackageLicenseKindValueProvider(storage);
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(new Dictionary<string, string?>
            {
                { "PackageLicenseExpression", expressionPropertyValue },
                { "PackageLicenseFile", filePropertyValue }
            });

            var kindValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, defaultProperties);

            Assert.Equal(expected: expectedValue, actual: kindValue);
        }

        [Theory]

        //          New value      Current     Current             Expected    Expected            Expected
        //                         Expression  File                Expression  File                stored value
        [InlineData("Expression",  null,       null,               null,       null,               "Expression")]
        [InlineData("Expression",  "alpha",    null,               "alpha",    null,               "Expression")]
        [InlineData("Expression",  null,       @"C:\license.txt",  null,       null,               "Expression")]
        [InlineData("File",        null,       null,               null,       null,               "File")]
        [InlineData("File",        "alpha",    @"C:\license.txt",  null,       @"C:\license.txt",  "File")]
        [InlineData("File",        "alpha",    null,               null,       null,               "File")]
        [InlineData("None",        "alpha",    null,               null,       null,               "None")]
        [InlineData("None",        null,       @"C:\license.txt",  null,       null,               "None")]
        public async Task SetPackageLicenseKind(string newValue, string? currentExpressionPropertyValue, string? currentFilePropertyValue, string? expectedExpressionPropertyValue, string? expectedFilePropertyValue, string? expectedStoredValue)
        {
            Dictionary<string, string> storageDictionary = new();
            var storage = ITemporaryPropertyStorageFactory.Create(storageDictionary);

            Dictionary<string, string?> defaultPropertiesDictionary = new();
            defaultPropertiesDictionary["PackageLicenseExpression"] = currentExpressionPropertyValue;
            defaultPropertiesDictionary["PackageLicenseFile"] = currentFilePropertyValue;
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(defaultPropertiesDictionary);

            var provider = new PackageLicenseKindValueProvider(storage);
            await provider.OnSetPropertyValueAsync("", newValue, defaultProperties);

            defaultPropertiesDictionary.TryGetValue("PackageLicenseExpression", out string? finalExpressionPropertyValue);
            defaultPropertiesDictionary.TryGetValue("PackageLicenseFile", out string? finalFilePropertyValue);
            storageDictionary.TryGetValue("PackageLicenseKind", out string? finalStoredValue);

            Assert.Equal(expectedExpressionPropertyValue, finalExpressionPropertyValue);
            Assert.Equal(expectedFilePropertyValue, finalFilePropertyValue);
            Assert.Equal(expectedStoredValue, finalStoredValue);
        }
    }
}
