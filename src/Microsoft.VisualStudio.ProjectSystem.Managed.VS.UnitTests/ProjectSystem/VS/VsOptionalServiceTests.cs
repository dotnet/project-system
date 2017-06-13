// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class VsOptionalServiceTests
    {
        [Fact]
        public void Constructor_NullAsServiceProvider_ThrowsArgumentNull()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();

            Assert.Throws<ArgumentNullException>("serviceProvider", () => {
                return new VsOptionalService<string, string>((IServiceProvider)null, threadingService);
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            var serviceProvider = SVsServiceProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("threadingService", () => {
                return new VsOptionalService<string, string>(serviceProvider, (IProjectThreadingService)null);
            });
        }

        [Fact]
        public void Value_MustBeCalledOnUIThread()
        {
            var threadingService = IProjectThreadingServiceFactory.ImplementVerifyOnUIThread(() => throw new InvalidOperationException());

            var service = CreateInstance<string, string>(threadingService: threadingService);

            Assert.Throws<InvalidOperationException>(() => {
                var value = service.Value;
            });
        }

        [Fact]
        public void Value_WhenMissingService_ReturnsNull()
        {
            var threadingService = IProjectThreadingServiceFactory.ImplementVerifyOnUIThread(() => { });
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => null);

            var service = CreateInstance<string, string>(serviceProvider: serviceProvider, threadingService: threadingService);

            var result = service.Value;

            Assert.Null(result);
        }

        [Fact]
        public void Value_ReturnsGetService()
        {
            object input = new object();

            var threadingService = IProjectThreadingServiceFactory.ImplementVerifyOnUIThread(() => { });
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => {
                if (type == typeof(string))
                    return input;

                return null;

            });

            var service = CreateInstance<string, object>(serviceProvider: serviceProvider, threadingService: threadingService);

            var result = service.Value;

            Assert.Same(input, result);
        }

        [Fact]
        public void Value_DoesNotCache()
        {
            var threadingService = IProjectThreadingServiceFactory.ImplementVerifyOnUIThread(() => { });
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => {
                return new object();
            });

            var service = CreateInstance<string, object>(serviceProvider: serviceProvider, threadingService: threadingService);

            var result1 = service.Value;
            var result2 = service.Value;

            Assert.NotSame(result1, result2);
        }

        private VsOptionalService<TService, TInterface> CreateInstance<TService, TInterface>(IServiceProvider serviceProvider = null, IProjectThreadingService threadingService = null)
        {
            serviceProvider = serviceProvider ?? SVsServiceProviderFactory.Create();
            threadingService = threadingService ?? IProjectThreadingServiceFactory.Create();

            return new VsOptionalService<TService, TInterface>(serviceProvider, threadingService);
        }
    }
}
