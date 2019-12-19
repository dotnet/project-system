// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IPhysicalProjectTreeFactory
    {
        public static IPhysicalProjectTree Create(IProjectTreeProvider? provider = null, IProjectTree? currentTree = null, IProjectTreeService? service = null, IPhysicalProjectTreeStorage? storage = null)
        {
            currentTree ??= ProjectTreeParser.Parse("Project");
            provider ??= new ProjectTreeProvider();
            storage ??= IPhysicalProjectTreeStorageFactory.Create();
            service ??= IProjectTreeServiceFactory.Create(currentTree, provider);

            var mock = new Mock<IPhysicalProjectTree>();
            mock.Setup(t => t.TreeProvider)
                .Returns(provider);

            mock.Setup(t => t.CurrentTree)
                .Returns(currentTree);

            mock.Setup(t => t.TreeService)
                .Returns(service);

            mock.Setup(t => t.TreeStorage)
           .Returns(storage);

            return mock.Object;
        }
    }
}
