// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IVsOptionalServiceFactory
    {
        public static IVsOptionalService<T> Create<T>(T value)
        {
            var mock = new Mock<IVsOptionalService<T>>();
            mock.SetupGet(s => s.Value)
                .Returns(() => value);

            return mock.Object;
        }

        public static IVsOptionalService<TService, TInterface> Create<TService, TInterface>(TInterface value)
        {
            var mock = new Mock<IVsOptionalService<TService, TInterface>>();
            mock.SetupGet(s => s.Value)
                .Returns(() => value);

            return mock.Object;
        }
    }
}