// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class CSharpProjectDesignerPageProviderTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            CreateInstance();
        }

        [Fact]
        public async Task GetPagesAsync_ReturnsPagesInOrder()
        {
            var provider = CreateInstance();
            var pages = await provider.GetPagesAsync();
            ProjectDesignerPageMetadata[] expectedPages = new ProjectDesignerPageMetadata[]
            {
                CSharpProjectDesignerPage.Application,
                CSharpProjectDesignerPage.Build,
                CSharpProjectDesignerPage.BuildEvents,
                CSharpProjectDesignerPage.Debug,
                CSharpProjectDesignerPage.Signing,
            };

            Assert.Equal(expectedPages.Length, pages.Count());
            for (int i = 0; i < pages.Count; i++)
                Assert.Same(expectedPages[i], pages.ElementAt(i));
        }

        [Fact]
        public async Task GetPagesAsync_WithPackCapability()
        {
            var provider = CreateInstance(ProjectCapability.Pack);
            var pages = await provider.GetPagesAsync();

            ProjectDesignerPageMetadata[] expectedPages = new ProjectDesignerPageMetadata[]
            {
                CSharpProjectDesignerPage.Application,
                CSharpProjectDesignerPage.Build,
                CSharpProjectDesignerPage.BuildEvents,
                CSharpProjectDesignerPage.Package,
                CSharpProjectDesignerPage.Debug,
                CSharpProjectDesignerPage.Signing,
            };

            Assert.Equal(expectedPages.Length, pages.Count());
            for (int i = 0; i < pages.Count; i++)
                Assert.Same(expectedPages[i], pages.ElementAt(i));
        }

        private static CSharpProjectDesignerPageProvider CreateInstance(params string[] capabilities)
        {
            Func<string, bool> containsCapability = c => capabilities.Contains(c);
            var capabilitiesService = IProjectCapabilitiesServiceFactory.ImplementsContains(containsCapability);
            return new CSharpProjectDesignerPageProvider(capabilitiesService);
        }
    }
}
