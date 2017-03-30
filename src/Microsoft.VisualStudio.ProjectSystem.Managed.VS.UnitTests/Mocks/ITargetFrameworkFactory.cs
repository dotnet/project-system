// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class ITargetFrameworkFactory
    {
        public static ITargetFramework Create()
        {
            return Mock.Of<ITargetFramework>();
        }

        public static ITargetFramework Implement(
            string shortName = null,
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<ITargetFramework>(behavior);            

            if (shortName != null)
            {
                mock.Setup(x => x.ShortName).Returns(shortName);
            }

            return mock.Object;
        }
    }
}