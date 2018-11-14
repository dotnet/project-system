// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class AbstractMultiLifetimeComponentTests
    {
        [Fact]
        public void PublishInstanceAsync_WhenNotUnPublishInstanceAsync_ReturnsNonCompletedTask()
        {
            var component = CreateInstance();

            var result = component.PublishInstanceAsync();

            Assert.False(result.IsCanceled);
            Assert.False(result.IsCompleted);
            Assert.False(result.IsFaulted);
        }

        [Fact]
        public async Task PublishInstanceAsync_WhenLoaded_ReturnsCompletedTask()
        {
            var component = CreateInstance();

            await component.LoadAsync();

            var result = component.PublishInstanceAsync();

            Assert.True(result.IsCompleted);
        }

        [Fact]
        public async Task PublishInstanceAsync_WhenUnloaded_ReturnsNonCompletedTask()
        {
            var component = CreateInstance();

            await component.LoadAsync();
            await component.UnloadAsync();

            var result = component.PublishInstanceAsync();

            Assert.False(result.IsCanceled);
            Assert.False(result.IsCompleted);
            Assert.False(result.IsFaulted);
        }

        [Fact]
        public async Task PublishInstanceAsync_DisposedWhenUnloaded_ReturnsCancelledTask()
        {
            var component = CreateInstance();

            await component.DisposeAsync();

            var result = component.PublishInstanceAsync();

            Assert.True(result.IsCanceled);
        }

        [Fact]
        public async Task PublishInstanceAsync_DisposedWhenLoaded_ReturnsCancelledTask()
        {
            var component = CreateInstance();

            await component.LoadAsync();
            await component.DisposeAsync();

            var result = component.PublishInstanceAsync();

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

            var result = await component.PublishInstanceAsync();

            Assert.True(result.IsInitialized);
        }

        [Fact]
        public async Task LoadAsync_WhenAlreadyLoaded_DoesNotCreateNewInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();

            var instance = await component.PublishInstanceAsync();

            await component.LoadAsync();

            var result = await component.PublishInstanceAsync();

            Assert.Same(instance, result);
        }

        [Fact]
        public async Task LoadAsync_WhenUnloaded_CreatesNewInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();

            var instance = await component.PublishInstanceAsync();

            await component.UnloadAsync();

            // We should create a new instance here
            await component.LoadAsync();

            var result = await component.PublishInstanceAsync();

            Assert.NotSame(instance, result);
        }

        [Fact]
        public async Task UnloadAsync_WhenLoaded_DisposesUnderlyingInstance()
        {
            var component = CreateInstance();

            await component.LoadAsync();
            var result = await component.PublishInstanceAsync();

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
            var instance = await component.PublishInstanceAsync();

            component.Dispose();

            Assert.True(instance.IsDisposed);
        }

        private static AbstractMultiLifetimeComponentFactory.MultiLifetimeComponent CreateInstance()
        {
            return AbstractMultiLifetimeComponentFactory.Create();
        }
    }
}
