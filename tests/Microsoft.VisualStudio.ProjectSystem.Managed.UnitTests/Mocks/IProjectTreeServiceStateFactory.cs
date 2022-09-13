// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectTreeServiceStateFactory
    {
        public static IProjectTreeServiceState Create()
        {
            return Mock.Of<IProjectTreeServiceState>();
        }

        public static IProjectTreeServiceState ImplementTreeProvider(Func<IProjectTreeProvider>? action)
        {
            return ImplementTree(null, action);
        }

        public static IProjectTreeServiceState ImplementTree(Func<IProjectTree?>? treeAction = null, Func<IProjectTreeProvider>? treeProviderAction = null)
        {
            var mock = new Mock<IProjectTreeServiceState>();

            if (treeAction is not null)
            {
                mock.SetupGet<IProjectTree?>(s => s.Tree)
                    .Returns(treeAction);
            }

            if (treeProviderAction is not null)
            {
                mock.SetupGet(s => s.TreeProvider)
                    .Returns(treeProviderAction);
            }

            return mock.Object;
        }
    }
}
