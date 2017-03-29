// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class FSharpProjectDesignerPageProviderTests
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

            Assert.Equal(pages.Count(), 5);
            Assert.Same(pages.ElementAt(0), FSharpProjectDesignerPage.Application);
            Assert.Same(pages.ElementAt(1), FSharpProjectDesignerPage.Build);
            Assert.Same(pages.ElementAt(2), FSharpProjectDesignerPage.BuildEvents);
            Assert.Same(pages.ElementAt(3), FSharpProjectDesignerPage.Debug);
            Assert.Same(pages.ElementAt(4), FSharpProjectDesignerPage.ReferencePaths);
        }

        [Fact]
        public async Task GetPagesAsync_WithPackCapability()
        {
            var provider = CreateInstance(ProjectCapability.Pack);
            var pages = await provider.GetPagesAsync();

            Assert.Equal(pages.Count(), 6);
            Assert.Same(pages.ElementAt(0), FSharpProjectDesignerPage.Application);
            Assert.Same(pages.ElementAt(1), FSharpProjectDesignerPage.Build);
            Assert.Same(pages.ElementAt(2), FSharpProjectDesignerPage.BuildEvents);
            Assert.Same(pages.ElementAt(3), FSharpProjectDesignerPage.Debug);
            Assert.Same(pages.ElementAt(4), FSharpProjectDesignerPage.Package);
            Assert.Same(pages.ElementAt(5), FSharpProjectDesignerPage.ReferencePaths);
        }

        private static FSharpProjectDesignerPageProvider CreateInstance(params string[] capabilities)
        {
            Func<string, bool> containsCapability = c => capabilities.Contains(c);
            var capabilitiesService = IProjectCapabilitiesServiceFactory.ImplementsContains(containsCapability);
            return new FSharpProjectDesignerPageProvider(capabilitiesService);
        }
    }
}
