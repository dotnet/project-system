// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class IProjectTreeServiceFactory
    {
        public static IProjectTreeService Create(IProjectTree tree)
        {
            var mock = new Mock<IProjectTreeService>();

            var treeState = IProjectTreeServiceStateFactory.ImplementTree(() => tree, () => IProjectTreeProviderFactory.Create());

            mock.SetupGet(s => s.CurrentTree)
                .Returns(treeState);

            return mock.Object;
        }

        public static IProjectTreeService Create()
        {
            return Mock.Of<IProjectTreeService>();
        }

        public static IProjectTreeService ImplementCurrentTree(Func<IProjectTreeServiceState> action)
        {
            var mock = new Mock<IProjectTreeService>();
            mock.SetupGet(s => s.CurrentTree)
                .Returns(action);

            return mock.Object;
        }
    }
}
