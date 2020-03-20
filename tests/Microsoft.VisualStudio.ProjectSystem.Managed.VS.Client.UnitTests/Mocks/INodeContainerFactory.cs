// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
