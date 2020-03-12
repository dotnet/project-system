// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public class VsServiceTests
    {
        [Fact]
        public void Constructor_NullAsServiceProvider_ThrowsArgumentNull()
        {
            var joinableTaskContext = IProjectThreadingServiceFactory.Create().JoinableTaskContext.Context;

            Assert.Throws<ArgumentNullException>("serviceProvider", () =>
            {
                return new VsService<string, string>(null!, joinableTaskContext);
            });
        }

        [Fact]
        public void Constructor_NullAsJoinableTaskContext_ThrowsArgumentNull()
        {
            var serviceProvider = IAsyncServiceProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("joinableTaskContext", () =>
            {
                return new VsService<string, string>(serviceProvider, null!);
            });
        }

        [Fact]
        public async Task GetValueAsync_CancelledToken_ThrowsOperationCanceled()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var serviceProvider = IAsyncServiceProviderFactory.ImplementGetServiceAsync(type => null);

            var service = CreateInstance<string, string>(serviceProvider: serviceProvider, threadingService: threadingService);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            {
                return service.GetValueAsync(new CancellationToken(true));
            });
        }

        [Fact]
        public async Task GetValueAsync_WhenMissingService_ReturnsNull()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var serviceProvider = IAsyncServiceProviderFactory.ImplementGetServiceAsync(type => null);

            var service = CreateInstance<string, string>(serviceProvider: serviceProvider, threadingService: threadingService);

            var result = await service.GetValueAsync();

            Assert.Null(result);
        }

        [Fact]
        public async Task GetValueAsync_ReturnsGetService()
        {
            object input = new object();

            var threadingService = IProjectThreadingServiceFactory.Create();
            var serviceProvider = IAsyncServiceProviderFactory.ImplementGetServiceAsync(type =>
            {
                if (type == typeof(string))
                    return input;

                return null;
            });

            var service = CreateInstance<string, object>(serviceProvider: serviceProvider, threadingService: threadingService);

            var result = await service.GetValueAsync();

            Assert.Same(input, result);
        }

        [Fact]
        public async Task GetValueAsync_CachesResult()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var serviceProvider = IAsyncServiceProviderFactory.ImplementGetServiceAsync(type =>
            {
                return new object();
            });

            var service = CreateInstance<string, object>(serviceProvider: serviceProvider, threadingService: threadingService);

            var result1 = await service.GetValueAsync();
            var result2 = await service.GetValueAsync();

            Assert.Same(result1, result2);
        }

        private static VsService<TService, TInterface> CreateInstance<TService, TInterface>(
            IAsyncServiceProvider? serviceProvider = null,
            IProjectThreadingService? threadingService = null)
            where TService : class
            where TInterface : class
        {
            serviceProvider ??= IAsyncServiceProviderFactory.Create();
            threadingService ??= IProjectThreadingServiceFactory.Create();

            return new VsService<TService, TInterface>(serviceProvider, threadingService.JoinableTaskContext.Context);
        }
    }
}
