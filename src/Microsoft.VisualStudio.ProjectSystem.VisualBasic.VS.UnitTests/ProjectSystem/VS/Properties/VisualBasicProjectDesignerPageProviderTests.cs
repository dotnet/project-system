// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class VisualBasicProjectDesignerPageProviderTests
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

            Assert.Equal(pages.Count(), 4);
            Assert.Same(pages.ElementAt(0), VisualBasicProjectDesignerPage.Application);
            Assert.Same(pages.ElementAt(1), VisualBasicProjectDesignerPage.Compile);
            Assert.Same(pages.ElementAt(2), VisualBasicProjectDesignerPage.References);
            Assert.Same(pages.ElementAt(3), VisualBasicProjectDesignerPage.Debug);
        }

        [Fact]
        public async Task GetPagesAsync_WithPackCapability()
        {
            var provider = CreateInstance(ProjectCapability.Pack);
            var pages = await provider.GetPagesAsync();

            Assert.Equal(pages.Count(), 5);
            Assert.Same(pages.ElementAt(0), VisualBasicProjectDesignerPage.Application);
            Assert.Same(pages.ElementAt(1), VisualBasicProjectDesignerPage.Compile);
            Assert.Same(pages.ElementAt(2), VisualBasicProjectDesignerPage.Package);
            Assert.Same(pages.ElementAt(3), VisualBasicProjectDesignerPage.References);
            Assert.Same(pages.ElementAt(4), VisualBasicProjectDesignerPage.Debug);
        }

        private static VisualBasicProjectDesignerPageProvider CreateInstance(params string[] capabilities)
        {
            Func<string, bool> containsCapability = c => capabilities.Contains(c);
            var capabilitiesService = IProjectCapabilitiesServiceFactory.ImplementsContains(containsCapability);
            return new VisualBasicProjectDesignerPageProvider(capabilitiesService);
        }
    }
}
