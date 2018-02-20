// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class IDependenciesChangesFactory
    {
        public static IDependenciesChanges Create()
        {
            return Mock.Of<IDependenciesChanges>();
        }

        public static IDependenciesChanges Implement(
            IEnumerable<IDependencyModel> addedNodes = null,
            IEnumerable<IDependencyModel> removedNodes = null,
            MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Strict;
            var mock = new Mock<IDependenciesChanges>(behavior);

            if (addedNodes != null)
            {
                mock.Setup(x => x.AddedNodes).Returns(ImmutableList<IDependencyModel>.Empty.AddRange(addedNodes));
            }

            if (removedNodes != null)
            {
                mock.Setup(x => x.RemovedNodes).Returns(ImmutableList<IDependencyModel>.Empty.AddRange(removedNodes));
            }

            return mock.Object;
        }        
    }
}
