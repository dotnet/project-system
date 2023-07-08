// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class AbstractMultiLifetimeComponentTests
    {
        [Fact]
        public void WaitForLoadedAsync_WhenNotLoadedAsync_ReturnsNonCompletedTask()
        {
            var component = CreateInstance();

            var result = component.WaitForLoadedAsync();

            Assert.False(result.IsCanceled);
            Assert.False(result.IsCompleted);
            Assert.False(result.IsFaulted);
        }

        [Fact]
        public async Task WaitForLoadedAsync_WhenLoaded_ReturnsCompletedTask()
        {
            var component = CreateInstance();

            await component.LoadAsync();

            var result = component.WaitForLoadedAsync();

            Assert.True(result.IsCompleted);
        }

        [Fact]
        public async Task WaitForLoadedAsync_WhenUnloaded_ReturnsNonCompletedTask()
        {
            var component = CreateInstance();

            await component.LoadAsync();
            await component.UnloadAsync();

            var result = component.WaitForLoadedAsync();

            Assert.False(result.IsCanceled);
            Assert.False(result.IsCompleted);
            Assert.False(result.IsFaulted);
        }

        [Fact]
        public async Task WaitForLoadedAsync_DisposedWhenUnloaded_ReturnsCancelledTask()
        {
            var component = CreateInstance();

            await component.DisposeAsync();

            var result = component.WaitForLoadedAsync();

            Assert.True(result.IsCanceled);
        }

        [Fact]
        public async Task WaitForLoadedAsync_DisposedWhenLoaded_ReturnsCancelledTask()
        {
            var component = CreateInstance();

            await component.LoadAsync();
            await component.DisposeAsync();

            var result = component.WaitForLoadedAsync();

            Assert.True(result.IsCanceled);
        }

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

            var result = await component.WaitForLoadedAsync();

            Assert.True(result.IsInitialized);
        }

        [Fact]
        public async Task LoadAsync_WhenAlreadyLoaded_DoesNotCreateNewInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();

            var instance = await component.WaitForLoadedAsync();

            await component.LoadAsync();

            var result = await component.WaitForLoadedAsync();

            Assert.Same(instance, result);
        }

        [Fact]
        public async Task LoadAsync_WhenUnloaded_CreatesNewInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();

            var instance = await component.WaitForLoadedAsync();

            await component.UnloadAsync();

            // We should create a new instance here
            await component.LoadAsync();

            var result = await component.WaitForLoadedAsync();

            Assert.NotSame(instance, result);
        }

        [Fact]
        public async Task UnloadAsync_WhenLoaded_DisposesUnderlyingInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();
            var result = await component.WaitForLoadedAsync();

            await component.UnloadAsync();

            Assert.True(result.IsDisposed);
        }

        [Fact]
        public async Task UnloadAsync_WhenNotLoaded_DoesNothing()
        {
            var component = CreateInstance();

            await component.UnloadAsync();
        }

        [Fact]
        public async Task UnloadAsync_DoesNotDispose()
        {
            var component = CreateInstance();

            await component.UnloadAsync();

            Assert.False(component.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_WhenNotLoaded_DoesNothing()
        {
            var component = CreateInstance();
            await component.DisposeAsync();

            Assert.True(component.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_WhenLoaded_DisposesUnderlyingInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();
            var instance = await component.WaitForLoadedAsync();

            await component.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        private static AbstractMultiLifetimeComponentFactory.MultiLifetimeComponent CreateInstance()
        {
            return AbstractMultiLifetimeComponentFactory.Create();
        }
    }
}
