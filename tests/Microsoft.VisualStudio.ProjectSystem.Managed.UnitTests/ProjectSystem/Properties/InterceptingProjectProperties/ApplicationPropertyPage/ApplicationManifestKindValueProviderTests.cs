// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class ApplicationManifestKindValueProviderTests
    {
        [Theory]
        //          ApplicationManifest              NoWin32Manifest Stored value       Expected value
        [InlineData("",                              "",             null,              "DefaultManifest")]
        [InlineData("",                              "",             "NoManifest",      "NoManifest")]
        [InlineData("",                              "",             "CustomManifest",  "CustomManifest")]
        [InlineData("",                              "true",         null,              "NoManifest")]
        [InlineData("",                              "false",        null,              "DefaultManifest")]
        [InlineData("",                              "false",        "CustomManifest",  "CustomManifest")]
        [InlineData(@"C:\alpha\beta\gamma.manifest", "",             null,              "CustomManifest")]
        [InlineData(@"C:\alpha\beta\gamma.manifest", "true",         null,              "CustomManifest")]
        [InlineData(@"C:\alpha\beta\gamma.manifest", "true",         "DefaultManifest", "CustomManifest")]
        public async Task GetApplicationManifestKind(string applicationManifestPropertyValue, string noManifestPropertyValue, string? storedValue, string expectedValue)
        {
            Dictionary<string, string>? storedValues = null;
            if (storedValue is not null)
            {
                storedValues = new Dictionary<string, string>
                {
                    { "ApplicationManifestKind", storedValue }
                };
            }
            var storage = ITemporaryPropertyStorageFactory.Create(storedValues);
            var provider = new ApplicationManifestKindValueProvider(storage);
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(new Dictionary<string, string?>
            {
                { "ApplicationManifest", applicationManifestPropertyValue },
                { "NoWin32Manifest", noManifestPropertyValue }
            });

            var kindValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, defaultProperties);

            Assert.Equal(expected: expectedValue, actual: kindValue);
        }

        [Theory]
        //          New value           Current                           Current          Expected     Expected         Expected
        //                              AppManifest                       NoWin32Manifest  AppManifest  NoWin32Manifest  stored value
        [InlineData("DefaultManifest",  @"C:\alpha\beta\gamma.manifest",  null,            null,        null,            "DefaultManifest")]
        [InlineData("DefaultManifest",  @"C:\alpha\beta\gamma.manifest",  "false",         null,        null,            "DefaultManifest")]
        [InlineData("DefaultManifest",  null,                             "true",          null,        null,            "DefaultManifest")]
        [InlineData("CustomManifest",   null,                             "true",          null,        null,            "CustomManifest")]
        [InlineData("NoManifest",       @"C:\alpha\beta\gamma.manifest",  null,            null,        "true",          "NoManifest")]
        [InlineData("NoManifest",       @"C:\alpha\beta\gamma.manifest",  "false",         null,        "true",          "NoManifest")]
        public async Task SetApplicationManifestKind(string newValue, string? currentApplicationManifestPropertyValue, string? currentNoManifestPropertyValue, string? expectedAppManifestPropertyValue, string? expectedNoManifestPropertyValue, string? expectedStoredValue)
        {
            Dictionary<string, string> storageDictionary = new();
            var storage = ITemporaryPropertyStorageFactory.Create(storageDictionary);

            Dictionary<string, string?> defaultPropertiesDictionary = new();
            defaultPropertiesDictionary["ApplicationManifest"] = currentApplicationManifestPropertyValue;
            defaultPropertiesDictionary["NoWin32Manifest"] = currentNoManifestPropertyValue;
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(defaultPropertiesDictionary);

            var provider = new ApplicationManifestKindValueProvider(storage);
            await provider.OnSetPropertyValueAsync("", newValue, defaultProperties);

            defaultPropertiesDictionary.TryGetValue("ApplicationManifest", out string? finalAppManifestPropertyValue);
            defaultPropertiesDictionary.TryGetValue("NoWin32Manifest", out string? finalNoManifestPropertyValue);
            storageDictionary.TryGetValue("ApplicationManifestKind", out string? finalStoredValue);

            Assert.Equal(expectedAppManifestPropertyValue, finalAppManifestPropertyValue);
            Assert.Equal(expectedNoManifestPropertyValue, finalNoManifestPropertyValue);
            Assert.Equal(expectedStoredValue, finalStoredValue);
        }
    }
}
