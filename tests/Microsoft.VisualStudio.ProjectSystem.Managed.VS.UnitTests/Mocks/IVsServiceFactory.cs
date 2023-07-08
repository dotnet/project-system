// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IVsServiceFactory
    {
        public static IVsService<T> Create<T>(T value) where T : class
        {
            var mock = new Mock<IVsService<T>>();
            mock.Setup(s => s.GetValueAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => value);

            return mock.Object;
        }

        public static IVsService<TService, TInterface> Create<TService, TInterface>(TInterface? value)
            where TService : class where TInterface : class
        {
            var mock = new Mock<IVsService<TService, TInterface>>();
            mock.Setup(s => s.GetValueAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => value!);
            mock.Setup(s => s.GetValueOrNullAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => value);

            return mock.Object;
        }
    }
}
