// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public class VsServiceTests
    {
        [Fact]
        public void Constructor_NullAsServiceProvider_ThrowsArgumentNull()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();

            Assert.Throws<ArgumentNullException>("serviceProvider", () => {

                return new VsService<string, string>((IServiceProvider)null, threadingService);
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            var serviceProvider = SVsServiceProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("threadingService", () => {

                return new VsService<string, string>(serviceProvider, (IProjectThreadingService)null);
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
        public void Value_WhenMissingService_Throws()
        {
            var threadingService = IProjectThreadingServiceFactory.ImplementVerifyOnUIThread(() => { });
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => null);

            var service = CreateInstance<string, string>(serviceProvider: serviceProvider, threadingService: threadingService);

            // We don't really care about the exception, it's an assertion
            Assert.ThrowsAny<Exception>(() => {
                var value = service.Value;
            });
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

            var service = CreateInstance<object, string>(serviceProvider: serviceProvider, threadingService: threadingService);

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

            var service = CreateInstance<object, string>(serviceProvider: serviceProvider, threadingService: threadingService);

            var result1 = service.Value;
            var result2 = service.Value;

            Assert.NotSame(result1, result2);
        }

        private VsService<TInterface, TService> CreateInstance<TInterface, TService>(IServiceProvider serviceProvider = null, IProjectThreadingService threadingService = null)
        {
            serviceProvider = serviceProvider ?? SVsServiceProviderFactory.Create();
            threadingService = threadingService ?? IProjectThreadingServiceFactory.Create();

            return new VsService<TInterface, TService>(serviceProvider, threadingService);
        }
    }
}
