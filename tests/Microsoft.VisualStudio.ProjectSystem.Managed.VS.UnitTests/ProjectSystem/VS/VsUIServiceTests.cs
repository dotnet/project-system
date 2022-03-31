// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public class VsUIServiceTests
    {
        [Fact]
        public void Constructor_NullAsServiceProvider_ThrowsArgumentNull()
        {
            var joinableTaskContext = new JoinableTaskContext();

            Assert.Throws<ArgumentNullException>("serviceProvider", () =>
            {
                return new VsUIService<string, string>(null!, joinableTaskContext);
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            var serviceProvider = SVsServiceProviderFactory.Create();

            Assert.Throws<ArgumentNullException>("joinableTaskContext", () =>
            {
                return new VsUIService<string, string>(serviceProvider, null!);
            });
        }

        [Fact]
        public async Task Value_MustBeCalledOnUIThread()
        {
            var service = CreateInstance<string, string>();

            var exception = await Assert.ThrowsAsync<COMException>(() =>
            {
                return Task.Run(() =>
                {
                    _ = service.Value;
                });
            });

            Assert.Equal(VSConstants.RPC_E_WRONG_THREAD, exception.HResult);
        }

        [Fact]
        public void Value_WhenMissingService_ReturnsNull()
        {
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => null);

            var service = CreateInstance<string, string>(serviceProvider: serviceProvider);

            var result = service.Value;

            Assert.Null(result);
        }

        [Fact]
        public void Value_ReturnsGetService()
        {
            object input = new object();

            var serviceProvider = IServiceProviderFactory.ImplementGetService(type =>
            {
                if (type == typeof(string))
                    return input;

                return null;
            });

            var service = CreateInstance<string, object>(serviceProvider: serviceProvider);

            var result = service.Value;

            Assert.Same(input, result);
        }

        [Fact]
        public void Value_CachesResult()
        {
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type =>
            {
                return new object();
            });

            var service = CreateInstance<string, object>(serviceProvider: serviceProvider);

            var result1 = service.Value;
            var result2 = service.Value;

            Assert.Same(result1, result2);
        }

        private static VsUIService<TService, TInterface> CreateInstance<TService, TInterface>(
            IServiceProvider? serviceProvider = null,
            JoinableTaskContext? joinableTaskContext = null)
            where TService : class
            where TInterface : class
        {
            serviceProvider ??= SVsServiceProviderFactory.Create();
            joinableTaskContext ??= new JoinableTaskContext();

            return new VsUIService<TService, TInterface>(serviceProvider, joinableTaskContext);
        }
    }
}
