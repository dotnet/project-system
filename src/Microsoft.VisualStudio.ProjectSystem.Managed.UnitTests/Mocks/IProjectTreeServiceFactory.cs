// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal class IProjectTreeServiceFactory
    {
        public static IProjectTreeService Create(IProjectTree tree)
        {
            var mock =  new Mock<IProjectTreeService>();
            
            var treeStateMock = new Mock<IProjectTreeServiceState>();
            treeStateMock.SetupGet(state => state.Tree)
                         .Returns(tree);
            treeStateMock.SetupGet(state => state.TreeProvider)
                         .Returns(IProjectTreeProviderFactory.Create());

            mock.SetupGet(s => s.CurrentTree)
                .Returns(treeStateMock.Object);

            return mock.Object;
        }
    }
}
