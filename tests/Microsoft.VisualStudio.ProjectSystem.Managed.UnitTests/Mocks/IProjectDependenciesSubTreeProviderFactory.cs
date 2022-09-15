// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectDependenciesSubTreeProviderFactory
    {
        public static IProjectDependenciesSubTreeProvider2 Implement(
            string? providerType = null,
            IDependencyModel? createRootDependencyNode = null,
            MockBehavior mockBehavior = MockBehavior.Strict,
            ProjectTreeFlags? groupNodeFlags = null)
        {
            var mock = new Mock<IProjectDependenciesSubTreeProvider2>(mockBehavior);

            if (providerType is not null)
            {
                mock.Setup(x => x.ProviderType).Returns(providerType);
            }

            if (createRootDependencyNode is not null)
            {
                mock.Setup(x => x.CreateRootDependencyNode()).Returns(createRootDependencyNode);
            }

            if (groupNodeFlags is not null)
            {
                mock.SetupGet(x => x.GroupNodeFlag).Returns(groupNodeFlags.Value);
            }

            return mock.Object;
        }
    }
}
