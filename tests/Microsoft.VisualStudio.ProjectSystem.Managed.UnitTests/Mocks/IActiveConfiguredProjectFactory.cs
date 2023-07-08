// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IActiveConfiguredValueFactory
    {
        public static IActiveConfiguredValue<T> ImplementValue<T>(Func<T> action)
            where T : class?
        {
            var mock = new Mock<IActiveConfiguredValue<T>>();

            mock.SetupGet(p => p.Value)
                .Returns(action);

            return mock.Object;
        }
    }
}
