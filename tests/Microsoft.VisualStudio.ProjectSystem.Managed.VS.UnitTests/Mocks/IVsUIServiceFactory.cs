// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

        public static IVsUIService<TService, TInterface> Create<TService, TInterface>(TInterface value) where TService : class
            where TInterface : class?
        {
            var mock = new Mock<IVsUIService<TService, TInterface>>();
            mock.SetupGet(s => s.Value)
                .Returns(() => value);

            return mock.Object;
        }
    }
}
