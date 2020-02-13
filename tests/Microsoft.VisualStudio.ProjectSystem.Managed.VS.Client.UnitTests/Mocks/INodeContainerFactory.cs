// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.Workspace.VSIntegration.UI
{
    internal static class INodeContainerFactory
    {
        public static INodeContainer Implement()
        {
            var mock = new Mock<INodeContainer>();
            return mock.Object;
        }
    }
}
