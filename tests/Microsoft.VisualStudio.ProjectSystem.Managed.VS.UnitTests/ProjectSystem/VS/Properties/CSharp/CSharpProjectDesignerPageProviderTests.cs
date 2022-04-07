// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
