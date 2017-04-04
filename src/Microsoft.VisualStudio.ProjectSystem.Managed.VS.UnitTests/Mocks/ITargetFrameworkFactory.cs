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
            string moniker = null,
            MockBehavior? mockBehavior = null)
        {
            return new TargetFramework(moniker);
        }
    }
}