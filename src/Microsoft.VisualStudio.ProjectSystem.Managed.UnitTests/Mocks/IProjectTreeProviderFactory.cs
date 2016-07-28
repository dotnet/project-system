// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class IProjectTreeProviderFactory
    {
        public static IProjectTreeProvider Create(string addNewItemDirectoryReturn = null)
        {
            var mock = new Mock<IProjectTreeProvider>();

            mock.Setup(t => t.GetPath(It.IsAny<IProjectTree>()))
                .Returns<IProjectTree>(tree => tree.FilePath);

            mock.Setup(t => t.RemoveAsync(It.IsAny<IImmutableSet<IProjectTree>>(), It.IsAny<DeleteOptions>()))
                .Returns<IImmutableSet<IProjectTree>, DeleteOptions>((nodes, options) => 
                {
                    foreach (var node in nodes)
                    {
                        node.Parent.Remove(node);
                    }
                    return Task.CompletedTask;
                });

            mock.Setup(t => t.GetAddNewItemDirectory(It.IsAny<IProjectTree>())).Returns(addNewItemDirectoryReturn);
            return mock.Object;
        }
    }
}
