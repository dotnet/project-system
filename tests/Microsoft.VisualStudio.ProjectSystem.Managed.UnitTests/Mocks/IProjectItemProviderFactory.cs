// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectItemProviderFactory
    {
        public static IProjectItemProvider Create()
        {
            return Mock.Of<IProjectItemProvider>();
        }

        public static IProjectItemProvider AddItemAsync(Func<string, IProjectItem> action)
        {
            var mock = new Mock<IProjectItemProvider>();
            mock.Setup(p => p.AddAsync(It.IsAny<string>()))
                .ReturnsAsync(action);

            return mock.Object;
        }

        public static IProjectItemProvider CreateWithAdd(IProjectTree inputTree)
        {
            var mock = new Mock<IProjectItemProvider>();

            mock.Setup(a => a.AddAsync(It.IsAny<string>()))
                .Returns<string>(path =>
               {
                   var fileName = Path.GetFileName(path);
                   var parentFolder = Path.GetDirectoryName(path);
                   var newSubTree = ProjectTreeParser.Parse($@"{fileName}, FilePath: ""{path}""");

                   // Find the node that has the parent folder and add the new node as a child.
                   foreach (var node in inputTree.GetSelfAndDescendentsBreadthFirst())
                   {
                       string? nodeFolderPath = node.IsFolder ? node.FilePath : Path.GetDirectoryName(node.FilePath);
                       if (nodeFolderPath?.TrimEnd(Path.DirectorySeparatorChar) == parentFolder)
                       {
                           if (node.TryFindImmediateChild(fileName, out IProjectTree? child) && !child.Flags.IsIncludedInProject())
                           {
                               var newFlags = child.Flags.Remove(ProjectTreeFlags.Common.IncludeInProjectCandidate);
                               child.SetProperties(flags: newFlags);
                           }
                           else
                           {
                               node.Add(newSubTree);
                           }
                           return Task.FromResult(Mock.Of<IProjectItem>());
                       }
                   }

                   return TaskResult.Null<IProjectItem>()!; // TODO remove ! when CPS annotations updated
               });

            return mock.Object;
        }

        public static IProjectItemProvider GetItemsAsync(Func<IEnumerable<IProjectItem>> action)
        {
            var mock = new Mock<IProjectItemProvider>();
            mock.Setup(p => p.GetItemsAsync(It.IsAny<string>()))
                .ReturnsAsync(action);

            return mock.Object;
        }
    }
}
