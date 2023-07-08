// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Imaging
{
    public class ProjectImageProviderAggregatorTests
    {
        [Fact]
        public void GetImageKey_NullAsKey_ThrowsArgumentNull()
        {
            var aggregator = CreateInstance();

            Assert.Throws<ArgumentNullException>("key", () =>
            {
                aggregator.GetProjectImage(null!);
            });
        }

        [Fact]
        public void GetImageKey_EmptyAsKey_ThrowsArgument()
        {
            var aggregator = CreateInstance();

            Assert.Throws<ArgumentException>("key", () =>
            {
                aggregator.GetProjectImage(string.Empty);
            });
        }

        [Fact]
        public void GetImageKey_WhenNoImageProviders_ReturnsNull()
        {
            var aggregator = CreateInstance();

            var result = aggregator.GetProjectImage("key");

            Assert.Null(result);
        }

        [Fact]
        public void GetImageKey_SingleImageProviderReturningNull_ReturnsNull()
        {
            var project = UnconfiguredProjectFactory.Create();
            var provider = IProjectImageProviderFactory.ImplementGetProjectImage((key) => null);
            var aggregator = CreateInstance(project);

            aggregator.ImageProviders.Add(provider);

            var result = aggregator.GetProjectImage("key");

            Assert.Null(result);
        }

        [Fact]
        public void GetImageKey_SingleImageProviderReturningKey_ReturnsKey()
        {
            var moniker = new ProjectImageMoniker(Guid.NewGuid(), 0);

            var project = UnconfiguredProjectFactory.Create();
            var provider = IProjectImageProviderFactory.ImplementGetProjectImage((key) => moniker);
            var aggregator = CreateInstance(project);

            aggregator.ImageProviders.Add(provider);

            var result = aggregator.GetProjectImage("key");

            Assert.Same(moniker, result);
        }

        [Fact]
        public void GetImageKey_ManyImageProviderReturningKey_ReturnsFirstByOrder()
        {
            var moniker1 = new ProjectImageMoniker(Guid.NewGuid(), 0);
            var moniker2 = new ProjectImageMoniker(Guid.NewGuid(), 0);

            var project = UnconfiguredProjectFactory.Create();
            var provider1 = IProjectImageProviderFactory.ImplementGetProjectImage((key) => moniker1);
            var provider2 = IProjectImageProviderFactory.ImplementGetProjectImage((key) => moniker2);
            var aggregator = CreateInstance(project);

            aggregator.ImageProviders.Add(provider2, orderPrecedence: 0);  // Lowest
            aggregator.ImageProviders.Add(provider1, orderPrecedence: 10); // Highest

            var result = aggregator.GetProjectImage("key");

            Assert.Same(moniker1, result);
        }

        [Fact]
        public void GetImageKey_ManyImageProviders_ReturnsFirstThatReturnsKey()
        {
            var moniker = new ProjectImageMoniker(Guid.NewGuid(), 0);

            var project = UnconfiguredProjectFactory.Create();
            var provider1 = IProjectImageProviderFactory.ImplementGetProjectImage((key) => null);
            var provider2 = IProjectImageProviderFactory.ImplementGetProjectImage((key) => moniker);
            var aggregator = CreateInstance(project);

            aggregator.ImageProviders.Add(provider1, orderPrecedence: 0);
            aggregator.ImageProviders.Add(provider2, orderPrecedence: 10);

            var result = aggregator.GetProjectImage("key");

            Assert.Same(moniker, result);
        }

        private static ProjectImageProviderAggregator CreateInstance(UnconfiguredProject? project = null)
        {
            project ??= UnconfiguredProjectFactory.Create();

            return new ProjectImageProviderAggregator(project);
        }
    }
}
