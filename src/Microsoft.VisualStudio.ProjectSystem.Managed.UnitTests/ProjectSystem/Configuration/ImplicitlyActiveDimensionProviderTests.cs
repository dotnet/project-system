// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    public class ImplicitlyActiveDimensionProviderTests
    {
        [Fact]
        public void GetImplicitlyActiveDimensions_NullAsDimensionNames_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            Assert.Throws<ArgumentNullException>("dimensionNames", () =>
            {
                provider.GetImplicitlyActiveDimensions((IEnumerable<string>)null);
            });
        }

        [Fact]
        public void GetImplicitlyActiveDimensions_EmptyAsDimensionNames_ReturnsEmpty()
        {
            var provider = CreateInstance();

            var result = provider.GetImplicitlyActiveDimensions(Enumerable.Empty<string>());

            Assert.Empty(result);
        }

        [Fact]
        public void GetImplicitlyActiveDimensions_WhenNoProviders_ReturnsEmpty()
        {
            var provider = CreateInstance();

            var result = provider.GetImplicitlyActiveDimensions(new string[] { "Configuration", "Platform" });

            Assert.Empty(result);
        }

        [Theory]    // Input                                    All Dimensions                                                  Variant dimensions                        Expected
        [InlineData("Configuration",                            "Configuration;Platform",                                       new[] { false, false },                   "")]
        [InlineData("Configuration",                            "Configuration;Platform",                                       new[] { true, false },                    "Configuration")]
        [InlineData("Configuration;Platform",                   "Configuration;Platform;TargetFramework",                       new[] { false, false, true },             "")]
        [InlineData("Configuration;Platform;TargetFramework",   "Configuration;Platform;TargetFramework",                       new[] { false, false, true },             "TargetFramework")]
        [InlineData("Configuration;Platform;TargetFramework",   "Configuration;Platform;TargetFramework",                       new[] { true, false, true },              "Configuration;TargetFramework")]
        [InlineData("Configuration;Platform;TargetFramework",   "Configuration;Platform;TargetFramework",                       new[] { true, true, true },               "Configuration;Platform;TargetFramework")]
        [InlineData("Configuration",                            "Configuration;Platform;TargetFramework",                       new[] { true, true, true },               "Configuration")]
        [InlineData("Configuration;Platform",                   "Configuration;Platform;TargetFramework",                       new[] { true, true, true },               "Configuration;Platform")]
        [InlineData("Configuration;Platform",                   "Configuration;Platform;TargetFramework;TargetFramework",       new[] { true, true, true, true },         "Configuration;Platform")]
        public void GetImplicitlyActiveDimensions_ReturnsImplicitlyActiveDimensions(string dimensionNames, string allDimensionsNames, bool[] isVariantDimension, string expected)
        {
            var metadata = IConfigurationDimensionDescriptionMetadataViewFactory.Create(allDimensionsNames.SplitReturningEmptyIfEmpty(';'), isVariantDimension);

            var provider = CreateInstance();
            provider.DimensionProviders.Add(new Lazy<IProjectConfigurationDimensionsProvider, IConfigurationDimensionDescriptionMetadataView>(metadata));

            var result = provider.GetImplicitlyActiveDimensions(dimensionNames.SplitReturningEmptyIfEmpty(';'));

            Assert.Equal(expected.SplitReturningEmptyIfEmpty(';'), result);
        }

        private static ImplicitlyActiveDimensionProvider CreateInstance()
        {
            var project = UnconfiguredProjectFactory.Create();

            return new ImplicitlyActiveDimensionProvider(project);
        }
    }
}
