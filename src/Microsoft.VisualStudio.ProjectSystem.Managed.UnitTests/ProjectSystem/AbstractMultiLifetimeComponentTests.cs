// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

using Xunit;
using static Microsoft.VisualStudio.ProjectSystem.AbstractMultiLifetimeComponentFactory.MultiLifetimeComponent;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Trait("UnitTest", "ProjectSystem")]
    public class AbstractMultiLifetimeComponentTests
    {
        [Fact]
        public async Task LoadAsync_Initializes()
        {
            var component = CreateInstance();

            await component.LoadAsync();

            Assert.True(component.IsInitialized);
        }

        [Fact]
        public async Task LoadAsync_InitializesUnderlyingInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();

            var result = (MultiLifetimeInstance)component.Instance;

            Assert.True(result.IsInitialized);
        }

        [Fact]
        public async Task LoadAsync_WhenAlreadyLoaded_DoesNotCreateNewInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();

            var instance = component.Instance;

            await component.LoadAsync();

            Assert.Same(instance, component.Instance);
        }

        [Fact]
        public async Task LoadAsync_WhenUnloaded_CreatesNewInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();

            var instance = component.Instance;

            await component.UnloadAsync();

            // We should create a new instance here
            await component.LoadAsync();

            Assert.NotSame(instance, component.Instance);
        }

        [Fact]
        public async Task UnloadAsync_WhenLoaded_DisposesUnderlyingInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();
            var instance = component.Instance;

            await component.UnloadAsync();

            Assert.Null(component.Instance);
            Assert.True(((MultiLifetimeInstance)instance).IsDisposed);
        }

        [Fact]
        public async Task UnloadAsync_WhenNotLoaded_DoesNothing()
        {
            var component = CreateInstance();

            await component.UnloadAsync();

            Assert.Null(component.Instance);
        }

        [Fact]
        public async Task UnloadAsync_DoesNotDispose()
        {
            var component = CreateInstance();

            await component.UnloadAsync();

            Assert.False(component.IsDisposed);
        }

        [Fact]
        public void Dispose_WhenNotLoaded_DoesNothing()
        {
            var component = CreateInstance();
            component.Dispose();

            Assert.True(component.IsDisposed);
        }

        [Fact]
        public async Task Dispose_WhenLoaded_DisposesUnderlyingInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();
            var instance = component.Instance;

            component.Dispose();

            Assert.True(((MultiLifetimeInstance)instance).IsDisposed);
        }

        private static AbstractMultiLifetimeComponentFactory.MultiLifetimeComponent CreateInstance()
        {
            return AbstractMultiLifetimeComponentFactory.Create();
        }
    }
}
