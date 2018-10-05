// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            var threadingService = IProjectThreadingServiceFactory.Create();

            Assert.Throws<ArgumentNullException>("serviceProvider", () =>
            {
                return new VsService<string, string>((IAsyncServiceProvider)null, threadingService);
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            var serviceProvider = IAsyncServiceProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("threadingService", () =>
            {
                return new VsService<string, string>(serviceProvider, (IProjectThreadingService)null);
            });
        }

        [Fact]
        public async Task Value_WhenMissingService_ReturnsNull()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var serviceProvider = IAsyncServiceProviderFactory.ImplementGetServiceAsync(type => null);

            var service = CreateInstance<string, string>(serviceProvider: serviceProvider, threadingService: threadingService);

            var result = await service.GetValueAsync();

            Assert.Null(result);
        }

        [Fact]
        public async Task Value_ReturnsGetService()
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
        public async Task Value_CachesResult()
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

        private VsService<TService, TInterface> CreateInstance<TService, TInterface>(IAsyncServiceProvider serviceProvider = null, IProjectThreadingService threadingService = null)
        {
            serviceProvider = serviceProvider ?? IAsyncServiceProviderFactory.Create();
            threadingService = threadingService ?? IProjectThreadingServiceFactory.Create();

            return new VsService<TService, TInterface>(serviceProvider, threadingService);
        }
    }
}
