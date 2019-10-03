// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.CSharp
{
    public class CSharpProjectDesignerPageProviderTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            CreateInstance();
        }

        [Fact]
        public async Task GetPagesAsync_WhenAllCapabilitiesPresent_ReturnsPagesInOrder()
        {
            var provider = CreateInstance(ProjectCapability.LaunchProfiles, ProjectCapability.Pack);
            var result = await provider.GetPagesAsync();

            var expected = ImmutableArray.Create<IPageMetadata>(
                CSharpProjectDesignerPage.Application,
                CSharpProjectDesignerPage.Build,
                CSharpProjectDesignerPage.BuildEvents,
                CSharpProjectDesignerPage.Package,
                CSharpProjectDesignerPage.Debug,
                CSharpProjectDesignerPage.Signing,
                CSharpProjectDesignerPage.CodeAnalysis
            );

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetPagesAsync_WhenNoLaunchProfilesCapability_DoesNotContainDebugPage()
        {
            var provider = CreateInstance();
            var result = await provider.GetPagesAsync();

            Assert.DoesNotContain(CSharpProjectDesignerPage.Debug, result);
        }

        [Fact]
        public async Task GetPagesAsync_WhenNoPackCapability_DoesNotContainPackagePage()
        {
            var provider = CreateInstance();
            var result = await provider.GetPagesAsync();

            Assert.DoesNotContain(CSharpProjectDesignerPage.Package, result);
        }

        private static CSharpProjectDesignerPageProvider CreateInstance(params string[] capabilities)
        {
            bool containsCapability(string c) => capabilities.Contains(c);
            var capabilitiesService = IProjectCapabilitiesServiceFactory.ImplementsContains(containsCapability);
            return new CSharpProjectDesignerPageProvider(capabilitiesService);
        }
    }
}
