// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Moq;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IPhysicalProjectTreeFactory
    {

        public static IPhysicalProjectTree Create(IProjectTreeProvider provider = null, IProjectTree currentTree = null, IProjectTreeService service = null)
        {
            currentTree ??= ProjectTreeParser.Parse("Project");
            provider ??= new ProjectTreeProvider();

            var mock = new Mock<IPhysicalProjectTree>();
            mock.Setup(t => t.TreeProvider)
                .Returns(provider);

            mock.Setup(t => t.CurrentTree)
                .Returns(currentTree);

            mock.Setup(t => t.TreeService)
                .Returns(service ?? IProjectTreeServiceFactory.Create(currentTree, provider));

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
