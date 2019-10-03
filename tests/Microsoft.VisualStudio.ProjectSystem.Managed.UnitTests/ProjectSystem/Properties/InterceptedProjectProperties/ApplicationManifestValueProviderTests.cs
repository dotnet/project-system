// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

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

            var appManifestValue = await provider.OnGetEvaluatedPropertyValueAsync(appManifestPropValue, defaultProperties);
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
            var provider = new ApplicationManifestValueProvider(UnconfiguredProjectFactory.Create(filePath: @"C:\projectdir\proj.proj"));
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(new Dictionary<string, string?>
                                                                                            {
                                                                                                { "ApplicationManifest", appManifestPropValue },
                                                                                                { "NoWin32Manifest", noManifestPropValue }
                                                                                            });

            var appManifestValue = await provider.OnSetPropertyValueAsync(valueToSet, defaultProperties);
            var noManifestValue = await defaultProperties.GetEvaluatedPropertyValueAsync("NoWin32Manifest");

            Assert.Equal(expectedAppManifestValue, appManifestValue);
            Assert.Equal(expectedNoManifestValue, noManifestValue);
        }
    }
}
