// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectTreeProviderFactory
    {
        public static IProjectTreeProvider ImplementGetAddNewItemDirectory(Func<IProjectTree, string> action)
        {
            var mock = new Mock<IProjectTreeProvider>();

            mock.Setup(p => p.GetPath(It.IsAny<IProjectTree>()))
                .Returns((IProjectTree tree) => tree.FilePath);

            mock.Setup(p => p.GetAddNewItemDirectory(It.IsAny<IProjectTree>()))
                .Returns(action);

            return mock.Object;
        }

        public static IProjectTreeProvider ImplementGetPath(Func<IProjectTree, string> action)
        {
            var mock = new Mock<IProjectTreeProvider>();
            mock.Setup(p => p.GetPath(It.IsAny<IProjectTree>()))
                .Returns(action);

            return mock.Object;
        }

        public static IProjectTreeProvider ImplementFindByPath(Func<IProjectTree, string, IProjectTree?> action)
        {
            var mock = new Mock<IProjectTreeProvider>();
            mock.Setup(p => p.FindByPath(It.IsAny<IProjectTree>(), It.IsAny<string>()))
                .Returns(action);

            return mock.Object;
        }

        public static IProjectTreeProvider Create(string? addNewItemDirectoryReturn = null, Func<IProjectTree, string, IProjectTree>? findByPathAction = null)
        {
            var mock = new Mock<IProjectTreeProvider>();

            var mockTree = Mock.Of<IReceivableSourceBlock<IProjectVersionedValue<IProjectTreeSnapshot>>>();
            mock.SetupGet(t => t.Tree)
                .Returns(mockTree);

            mock.Setup(t => t.GetPath(It.IsAny<IProjectTree>()))
                .Returns<IProjectTree>(tree => tree.FilePath);

            mock.Setup(t => t.RemoveAsync(It.IsAny<IImmutableSet<IProjectTree>>(), It.IsAny<DeleteOptions>()))
                .Returns<IImmutableSet<IProjectTree>, DeleteOptions>((nodes, options) =>
                {
                    foreach (var node in nodes)
                    {
                        node.Parent!.Remove(node);
                    }
                    return Task.CompletedTask;
                });

            mock.Setup(t => t.GetAddNewItemDirectory(It.IsAny<IProjectTree>())).Returns(addNewItemDirectoryReturn);

            mock.Setup(p => p.FindByPath(It.IsAny<IProjectTree>(), It.IsAny<string>()))
                .Returns(findByPathAction);

            return mock.Object;
        }
    }
}
