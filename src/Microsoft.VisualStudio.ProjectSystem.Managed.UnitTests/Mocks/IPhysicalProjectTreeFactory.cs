// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IPhysicalProjectTreeFactory
    {
        public static IPhysicalProjectTree Create(IProjectTreeProvider provider = null)
        {
            var mock = new Mock<IPhysicalProjectTree>();
            mock.Setup(t => t.TreeProvider).Returns(provider ?? IProjectTreeProviderFactory.Create());
            
            return mock.Object;
        }
    }
}
