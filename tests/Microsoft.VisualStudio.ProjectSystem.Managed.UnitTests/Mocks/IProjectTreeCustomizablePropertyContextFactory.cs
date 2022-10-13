// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectTreeCustomizablePropertyContextFactory
    {
        public static IProjectTreeCustomizablePropertyContext Create()
        {
            return Mock.Of<IProjectTreeCustomizablePropertyContext>();
        }

        public static IProjectTreeCustomizablePropertyContext Implement(
            string? itemName = null,
            string? itemType = null,
            bool isFolder = false,
            ProjectTreeFlags flags = default,
            IImmutableDictionary<string, string>? metadata = null)
        {
            var mock = new Mock<IProjectTreeCustomizablePropertyContext>();
            mock.Setup(x => x.ItemName).Returns(itemName ?? string.Empty);
            mock.Setup(x => x.ItemType).Returns(itemType);
            mock.Setup(x => x.IsFolder).Returns(isFolder);
            mock.Setup(x => x.ParentNodeFlags).Returns(flags);
            mock.Setup(x => x.Metadata).Returns(metadata ?? ImmutableStringDictionary<string>.EmptyOrdinal);
            return mock.Object;
        }
    }
}
