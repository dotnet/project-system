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

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            var pages = await provider.GetPagesAsync();
#pragma warning restore RS0003 // Do not directly await a Task

            Assert.Equal(pages.Count(), 3);
            Assert.Same(pages.ElementAt(0), VisualBasicProjectDesignerPage.Application);
            Assert.Same(pages.ElementAt(1), VisualBasicProjectDesignerPage.References);
            Assert.Same(pages.ElementAt(2), VisualBasicProjectDesignerPage.Debug);
        }

        [Fact]
        public async Task GetPagesAsync_WithPackCapability()
        {
            var provider = CreateInstance(ProjectCapability.Pack);

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            var pages = await provider.GetPagesAsync();
#pragma warning restore RS0003 // Do not directly await a Task

            Assert.Equal(pages.Count(), 4);
            Assert.Same(pages.ElementAt(0), VisualBasicProjectDesignerPage.Application);
            Assert.Same(pages.ElementAt(1), VisualBasicProjectDesignerPage.Package);
            Assert.Same(pages.ElementAt(2), VisualBasicProjectDesignerPage.References);
            Assert.Same(pages.ElementAt(3), VisualBasicProjectDesignerPage.Debug);
        }

        private static VisualBasicProjectDesignerPageProvider CreateInstance(params string[] capabilities)
        {
            Func<string, bool> containsCapability = c => capabilities.Contains(c);
            var capabilitiesService = IProjectCapabilitiesServiceFactory.ImplementsContains(containsCapability);
            return new VisualBasicProjectDesignerPageProvider(capabilitiesService);
        }
    }
}
