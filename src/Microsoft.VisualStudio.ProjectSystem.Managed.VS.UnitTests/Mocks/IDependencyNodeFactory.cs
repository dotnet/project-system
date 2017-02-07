//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Collections.Generic;
//using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
//using Moq;
//using Newtonsoft.Json.Linq;

//namespace Microsoft.VisualStudio.ProjectSystem.VS
//{
//    internal class IDependencyNodeFactory
//    {
//        public static IDependencyNode Create()
//        {
//            return Mock.Of<IDependencyNode>();
//        }

//        public static IDependencyNode Implement(IEnumerable<IDependencyNode> children = null,
//                                                MockBehavior? mockBehavior = null)
//        {
//            var behavior = mockBehavior ?? MockBehavior.Default;
//            var mock = new Mock<IDependencyNode>(behavior);

//            if (children != null)
//            {
//                mock.Setup(x => x.Children).Returns(new HashSet<IDependencyNode>(children));
//            }

//            mock.Setup(x => x.Children).Returns(builder.ToImmutableHashSet());

//            return mock.Object;
//        }

//        public static IDependencyNode Implement(string childrenJson = null,
//                                                MockBehavior? mockBehavior = null)
//        {
//            var behavior = mockBehavior ?? MockBehavior.Default;
//            var mock = new Mock<IDependencyNode>(behavior);

//            if (childrenJson != null)
//            {
//                var json = JObject.Parse(childrenJson);
//                var children = json.ToObject<DependenciesNodeCollection>();
//                mock.Setup(x => x.Children).Returns(new HashSet<IDependencyNode>(children.Nodes));
//            }
//            mock.Setup(x => x.Children).Returns(builder.ToImmutableHashSet());

//            return mock.Object;
//        }

//        public static IDependencyNode FromJson(string jsonString, ProjectTreeFlags? flags = null)
//        {
//            if (string.IsNullOrEmpty(jsonString))
//            {
//                return null;
//            }

//            var json = JObject.Parse(jsonString);
//            var data = json.ToObject<DependencyNode>();

//            if (flags != null && flags.HasValue)
//            {
//                data.Flags = data.Flags.Union(flags.Value);
//            }
//            return data;
//        }

//        private class DependenciesNodeCollection
//        {
//            public IEnumerable<DependencyNode> Nodes { get; set; }
//        }
//    }
//}