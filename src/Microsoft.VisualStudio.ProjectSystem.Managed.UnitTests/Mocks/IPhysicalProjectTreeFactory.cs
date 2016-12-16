// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IPhysicalProjectTreeFactory
    {

        public static IPhysicalProjectTree Create(IProjectTreeProvider provider = null, IProjectTree currentTree = null, IProjectTreeService service = null)
        {
            var mock = new Mock<IPhysicalProjectTree>();
            mock.Setup(t => t.TreeProvider)
                .Returns(provider ?? IProjectTreeProviderFactory.Create());

            mock.Setup(t => t.CurrentTree)
                .Returns(currentTree ?? ProjectTreeParser.Parse("Project"));

            mock.Setup(t => t.TreeService)
                .Returns(service ?? IProjectTreeServiceFactory.Create());

            return mock.Object;
        }

        public static IPhysicalProjectTree ImplementCurrentTree(Func<IProjectTree> action)
        {
            var mock = new Mock<IPhysicalProjectTree>();
            mock.Setup(t => t.CurrentTree)
                .Returns(action);

            return mock.Object;
        }
    }
}
