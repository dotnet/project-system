// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class INuGetPackagesDataProviderFactory
    {
        public static INuGetPackagesDataProvider Create()
        {
            return Mock.Of<INuGetPackagesDataProvider>();
        }

        public static INuGetPackagesDataProvider ImplementUpdateNodeChildren(string itemSpec,
                                                                IDependencyNode node,
                                                                IDependencyNode[] childrenToAdd,
                                                                MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<INuGetPackagesDataProvider>(behavior);
            mock.Setup(x => x.UpdateNodeChildren(itemSpec, node));

            childrenToAdd.ToList().ForEach(x => node.AddChild(x));

            return mock.Object;
        }

        public static INuGetPackagesDataProvider ImplementSearchAsync(string itemSpec,
                                                                      string searchTerm,
                                                                      IEnumerable<IDependencyNode> resultNodes,
                                                                      MockBehavior? mockBehavior = null)
        {
            var behavior = mockBehavior ?? MockBehavior.Default;
            var mock = new Mock<INuGetPackagesDataProvider>(behavior);
            mock.Setup(x => x.SearchAsync(itemSpec, searchTerm)).Returns(Task.FromResult(resultNodes));

            return mock.Object;
        }
    }
}