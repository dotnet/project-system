// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Moq;

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

        public static IVsService<TService, TInterface> Create<TService, TInterface>(TInterface? value) where TService : class where TInterface : class
        {
            var mock = new Mock<IVsService<TService, TInterface>>();
            mock.Setup(s => s.GetValueAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => value);

            return mock.Object;
        }
    }
}
