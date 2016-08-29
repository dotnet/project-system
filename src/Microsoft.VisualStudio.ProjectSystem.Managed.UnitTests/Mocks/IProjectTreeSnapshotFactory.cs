// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class IProjectTreeSnapshotFactory
    {
        public static IProjectTreeSnapshot Create(IProjectTree tree)
        {
            var mock = new Mock<IProjectTreeSnapshot>();

            mock.SetupGet(s => s.Tree).Returns(tree);

            return mock.Object;
        }
    }
}
