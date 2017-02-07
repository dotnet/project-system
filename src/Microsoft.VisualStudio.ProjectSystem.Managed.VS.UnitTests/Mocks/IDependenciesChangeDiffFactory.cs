//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Collections.Generic;
//using System.Collections.Immutable;
//using Moq;
//using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

//namespace Microsoft.VisualStudio.ProjectSystem.VS
//{
//    internal static class IDependenciesChangeDiffFactory
//    {
//        public static IDependenciesChangeDiff Create()
//        {
//            return Mock.Of<IDependenciesChangeDiff>();
//        }

//        public static IDependenciesChangeDiff Implement(IEnumerable<IDependencyNode> addedItems = null,
//                                                   IEnumerable<IDependencyNode> changedItems = null,
//                                                   IEnumerable<IDependencyNode> removedItems = null,
//                                                   MockBehavior? mockBehavior = null)
//        {
//            var behavior = mockBehavior ?? MockBehavior.Default;
//            var mock = new Mock<IDependenciesChangeDiff>(behavior);

//            if (addedItems != null)
//            {
//                mock.Setup(x => x.AddedNodes).Returns(addedItems.ToImmutableList());
//            }

//            if (changedItems != null)
//            {
//                mock.Setup(x => x.UpdatedNodes).Returns(changedItems.ToImmutableList());
//            }

//            if (removedItems != null)
//            {
//                mock.Setup(x => x.RemovedNodes).Returns(removedItems.ToImmutableList());
//            }

//            return mock.Object;
//        }
//    }
//}