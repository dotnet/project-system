// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IActiveConfiguredValuesFactory
    {
        public static IActiveConfiguredValues<T> ImplementValues<T>(Func<OrderPrecedenceImportCollection<T>> action)
            where T : class
        {
            var mock = new Mock<IActiveConfiguredValues<T>>();

            mock.SetupGet(p => p.Values)
                .Returns(action);

            return mock.Object;
        }
    }
}
