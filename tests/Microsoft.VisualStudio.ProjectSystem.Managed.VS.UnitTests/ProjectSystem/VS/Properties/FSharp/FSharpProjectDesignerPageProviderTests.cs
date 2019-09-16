// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.FSharp
{
    public class FSharpProjectDesignerPageProviderTests
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
               FSharpProjectDesignerPage.Application,
               FSharpProjectDesignerPage.Build,
               FSharpProjectDesignerPage.BuildEvents,
               FSharpProjectDesignerPage.Debug,
               FSharpProjectDesignerPage.Package,
               FSharpProjectDesignerPage.ReferencePaths
            );

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetPagesAsync_WhenNoLaunchProfilesCapability_DoesNotContainDebugPage()
        {
            var provider = CreateInstance();
            var result = await provider.GetPagesAsync();

            Assert.DoesNotContain(FSharpProjectDesignerPage.Debug, result);
        }

        [Fact]
        public async Task GetPagesAsync_WhenNoPackCapability_DoesNotContainPackagePage()
        {
            var provider = CreateInstance();
            var result = await provider.GetPagesAsync();

            Assert.DoesNotContain(FSharpProjectDesignerPage.Package, result);
        }

        private static FSharpProjectDesignerPageProvider CreateInstance(params string[] capabilities)
        {
            bool containsCapability(string c) => capabilities.Contains(c);
            var capabilitiesService = IProjectCapabilitiesServiceFactory.ImplementsContains(containsCapability);
            return new FSharpProjectDesignerPageProvider(capabilitiesService);
        }
    }
}
