// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IVsServiceFactory
    {
        public static IVsService<T> Create<T>(T value)
        {
            var mock = new Mock<IVsService<T>>();
            mock.SetupGet(s => s.Value)
                .Returns(() => value);

            return mock.Object;
        }

        public static IVsService<TService, TInterface> Create<TService, TInterface>(TInterface value)
        {
            var mock = new Mock<IVsService<TService, TInterface>>();
            mock.SetupGet(s => s.Value)
                .Returns(() => value);

            return mock.Object;
        }
    }
}