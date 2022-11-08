// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class ApplicationManifestValueProviderTests
    {
        [Theory]
        [InlineData(@"C:\somepath", "true", @"C:\somepath")]
        [InlineData(@"Invalidpath/\/", "true", @"Invalidpath/\/")]
        [InlineData("", "true", "NoManifest")]
        [InlineData("", "TRue", "NoManifest")]
        [InlineData("", "false", "DefaultManifest")]
        [InlineData("", null, "DefaultManifest")]
        public async Task GetApplicationManifest(string appManifestPropValue, string noManifestValue, string expectedValue)
        {
            var provider = new ApplicationManifestValueProvider(UnconfiguredProjectFactory.Create());
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue("NoWin32Manifest", noManifestValue);

            var appManifestValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, appManifestPropValue, defaultProperties);
            Assert.Equal(expectedValue, appManifestValue);
        }

        [Theory]
        [InlineData(@"inp.man", "true", @"out.man", @"out.man", "")]
        [InlineData(@"C:\projectdir\foo.man", "true", @"C:\projectdir\bar.man", @"bar.man", "")]
        [InlineData(@"C:\projectdir\foo.man", "true", @" a asd ", @" a asd ", "")]
        [InlineData(@"C:\projectdir\foo.man", null, @"NoManifest", null, "true")]
        [InlineData(@"C:\projectdir\foo.man", null, @"nomANifest", null, "true")]
        [InlineData(@"C:\projectdir\foo.man", null, @"DefaultManifest", null, "")]
        [InlineData(@"C:\projectdir\foo.man", null, "", null, "")]
        [InlineData(@"C:\projectdir\foo.man", null, null, null, "")]
        public async Task SetApplicationManifest(string appManifestPropValue, string? noManifestPropValue, string? valueToSet, string? expectedAppManifestValue, string expectedNoManifestValue)
        {
            var provider = new ApplicationManifestValueProvider(UnconfiguredProjectFactory.Create(fullPath: @"C:\projectdir\proj.proj"));
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(new Dictionary<string, string?>
                                                                                            {
                                                                                                { "ApplicationManifest", appManifestPropValue },
                                                                                                { "NoWin32Manifest", noManifestPropValue }
                                                                                            });

            var appManifestValue = await provider.OnSetPropertyValueAsync(string.Empty, valueToSet, defaultProperties);
            var noManifestValue = await defaultProperties.GetEvaluatedPropertyValueAsync("NoWin32Manifest");

            Assert.Equal(expectedAppManifestValue, appManifestValue);
            Assert.Equal(expectedNoManifestValue, noManifestValue);
        }
    }
}
