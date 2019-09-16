// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectDependenciesSubTreeProviderFactory
    {
        public static IProjectDependenciesSubTreeProvider Implement(
            string? providerType = null,
            IDependencyModel? createRootDependencyNode = null,
            MockBehavior mockBehavior = MockBehavior.Strict)
        {
            var mock = new Mock<IProjectDependenciesSubTreeProvider>(mockBehavior);

            if (providerType != null)
            {
                mock.Setup(x => x.ProviderType).Returns(providerType);
            }

            if (createRootDependencyNode != null)
            {
                mock.Setup(x => x.CreateRootDependencyNode()).Returns(createRootDependencyNode);
            }

            return mock.Object;
        }

        public static IProjectDependenciesSubTreeProviderInternal ImplementInternal(
            string? providerType = null,
            ImageMoniker implicitIcon = default,
            MockBehavior mockBehavior = MockBehavior.Strict)
        {
            var mock = new Mock<IProjectDependenciesSubTreeProviderInternal>(mockBehavior);

            if (providerType != null)
            {
                mock.Setup(x => x.ProviderType).Returns(providerType);
            }

            if (implicitIcon.Id != 0 || implicitIcon.Guid != Guid.Empty)
            {
                mock.Setup(x => x.ImplicitIcon).Returns(implicitIcon);
            }

            return mock.Object;
        }
    }
}
