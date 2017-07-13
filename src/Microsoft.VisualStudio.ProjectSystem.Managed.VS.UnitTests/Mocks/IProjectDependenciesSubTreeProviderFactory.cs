// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IProjectDependenciesSubTreeProviderFactory
    {
        public static IProjectDependenciesSubTreeProvider Create()
        {
            return Mock.Of<IProjectDependenciesSubTreeProvider>();
        }

        public static IProjectDependenciesSubTreeProvider Implement(
            string providerType = null,
            IDependency createRootDependencyNode = null,
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Strict;
            var mock = new Mock<IProjectDependenciesSubTreeProvider>(behavior);

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
            string providerType = null,
            ImageMoniker icon = new ImageMoniker(),
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Strict;
            var mock = new Mock<IProjectDependenciesSubTreeProviderInternal>(behavior);

            if (providerType != null)
            {
                mock.Setup(x => x.ProviderType).Returns(providerType);
            }

            if (icon.Id != 0 || icon.Guid != Guid.Empty)
            {
                mock.Setup(x => x.GetImplicitIcon()).Returns(icon);
            }

            return mock.Object;
        }

    }
}