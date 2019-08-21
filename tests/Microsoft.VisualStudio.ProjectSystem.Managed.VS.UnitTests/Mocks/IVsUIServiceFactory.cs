// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IVsUIServiceFactory
    {
        public static IVsUIService<T> Create<T>(T value) where T : class
        {
            var mock = new Mock<IVsUIService<T>>();
            mock.SetupGet(s => s.Value)
                .Returns(() => value);

            return mock.Object;
        }

        public static IVsUIService<TService, TInterface> Create<TService, TInterface>(TInterface? value) where TService : class where TInterface : class
        {
            var mock = new Mock<IVsUIService<TService, TInterface>>();
            mock.SetupGet(s => s.Value)
                .Returns(() => value);

            return mock.Object;
        }
    }
}
