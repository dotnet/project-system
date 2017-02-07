//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
//using Moq;

//namespace Microsoft.VisualStudio.ProjectSystem.VS
//{
//    internal class DependenciesChangeFactory
//    {
//        public static IDependencyNode Create()
//        {
//            return Mock.Of<IDependencyNode>();
//        }

//        public static DependenciesChange FromJson(string jsonString)
//        {
//            var model = new DependenciesChangeModel();
//            return model.FromJson(jsonString);
//        }

//        public static bool AreEqual(DependenciesChange left, DependenciesChange right)
//        {
//            if (left == null || right == null)
//            {
//                return left == right;
//            }

//            if (left == right)
//            {
//                return true;
//            }

//            return CollectionsExtensions.AreEqual(left.AddedNodes, right.AddedNodes)
//                && CollectionsExtensions.AreEqual(left.UpdatedNodes, right.UpdatedNodes)
//                && CollectionsExtensions.AreEqual(left.RemovedNodes, right.RemovedNodes);
//        }
//    }

//    internal class DependenciesChangeModel : JsonModel<DependenciesChange>
//    {
//        public List<DependencyNode> AddedNodes { get; set; }
//        public List<DependencyNode> UpdatedNodes { get; set; }
//        public List<DependencyNode> RemovedNodes { get; set; }

//        public override DependenciesChange ToActualModel()
//        {
//            return new TestableDependenciesChange(AddedNodes, UpdatedNodes, RemovedNodes);
//        }

//        private class TestableDependenciesChange : DependenciesChange
//        {
//            public TestableDependenciesChange(IEnumerable<IDependencyNode> addedNodes,
//                                              IEnumerable<IDependencyNode> updatedNodes,
//                                              IEnumerable<IDependencyNode> removedNodes)
//            {
//                AddedNodes = addedNodes.ToList();
//                UpdatedNodes = updatedNodes.ToList();
//                RemovedNodes = removedNodes.ToList();
//            }
//        }
//    }

//}