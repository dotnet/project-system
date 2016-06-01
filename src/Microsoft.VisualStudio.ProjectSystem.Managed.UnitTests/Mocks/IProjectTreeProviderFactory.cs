// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class IProjectTreeProviderFactory
    {
        public static IProjectTreeProvider Create()
        {
            return Mock.Of<IProjectTreeProvider>();
        }

        public static IProjectTreeProvider CreateWithGetPath()
        {
            var mock = new Mock<IProjectTreeProvider>();

            mock.Setup(t => t.GetPath(It.IsAny<IProjectTree>()))
                .Returns<IProjectTree>(tree => tree.FilePath);

            return mock.Object;
        }
    }
}
