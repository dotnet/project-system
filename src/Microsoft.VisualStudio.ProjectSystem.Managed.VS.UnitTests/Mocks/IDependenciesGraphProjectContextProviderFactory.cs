//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Collections.Generic;
//using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
//using Moq;

//namespace Microsoft.VisualStudio.ProjectSystem.VS
//{
//    internal static class IDependenciesGraphProjectContextProviderFactory
//    {
//        public static IDependenciesGraphProjectContextProvider Create()
//        {
//            return Mock.Of<IDependenciesGraphProjectContextProvider>();
//        }

//        public static IDependenciesGraphProjectContextProvider Implement(
//                                string projectPath,
//                                IProjectDependenciesSubTreeProvider subTreeProvider = null,
//                                IEnumerable<IProjectDependenciesSubTreeProvider> subTreeProviders = null,
//                                MockBehavior? mockBehavior = null)
//        {
//            var behavior = mockBehavior ?? MockBehavior.Default;
//            var mock = new Mock<IDependenciesGraphProjectContextProvider>(behavior);
//            var mockProjectContext = new Mock<IDependenciesGraphProjectContext>();

//            mockProjectContext.Setup(x => x.GetProvider("MyProvider")).Returns(subTreeProvider);
//            mock.Setup(x => x.GetProjectContext(projectPath)).Returns(mockProjectContext.Object);

//            if (subTreeProviders != null)
//            {
//                mockProjectContext.Setup(x => x.ProjectFilePath).Returns(projectPath);
//                mockProjectContext.Setup(x => x.GetProviders()).Returns(subTreeProviders);
//                mock.Setup(x => x.GetProjectContexts()).Returns(new[] { mockProjectContext.Object });
//            }

//            return mock.Object;
//        }

//        public static IDependenciesGraphProjectContextProvider ImplementMultipleProjects(
//                                IDictionary<string, IProjectDependenciesSubTreeProvider> contexts,
//                                MockBehavior? mockBehavior = null)
//        {
//            var behavior = mockBehavior ?? MockBehavior.Default;
//            var mock = new Mock<IDependenciesGraphProjectContextProvider>(behavior);
//            var listOfAllContexts = new List<IDependenciesGraphProjectContext>();

//            foreach(var kvp in contexts)
//            {
//                var mockProjectContext = new Mock<IDependenciesGraphProjectContext>();
//                mock.Setup(x => x.GetProjectContext(kvp.Key)).Returns(mockProjectContext.Object);
//                mockProjectContext.Setup(x => x.GetProvider("MyProvider")).Returns(kvp.Value);
//                listOfAllContexts.Add(mockProjectContext.Object);
//            }

//            mock.Setup(x => x.GetProjectContexts()).Returns(listOfAllContexts);

//            return mock.Object;
//        }

//        public static IDependenciesGraphProjectContext ImplementProjectContext(
//                        string projectPath,
//                        MockBehavior? mockBehavior = null)
//        {
//            var behavior = mockBehavior ?? MockBehavior.Default;
//            var mock = new Mock<IDependenciesGraphProjectContext>(behavior);

//            mock.Setup(x => x.ProjectFilePath).Returns(projectPath);

//            return mock.Object;
//        }
//    }
//}