// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class ITargetFrameworkProviderFactory
    {
        public static ITargetFrameworkProvider Create()
        {
            return Mock.Of<ITargetFrameworkProvider>();
        }

        public static ITargetFrameworkProvider Implement(
            ITargetFramework getNearestFramework = null,
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<ITargetFrameworkProvider>(behavior);            

            if (getNearestFramework != null)
            {
                mock.Setup(x => x.GetNearestFramework(It.IsAny<ITargetFramework>(), It.IsAny<IEnumerable<ITargetFramework>>()))
                    .Returns(getNearestFramework);
            }

            return mock.Object;
        }
    }
}
