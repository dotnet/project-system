// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectTreeCustomizablePropertyContextFactory
    {
        public static IProjectTreeCustomizablePropertyContext Implement(
            string? itemName = null,
            string? itemType = null,
            bool isFolder = false,
            ProjectTreeFlags flags = default,
            IImmutableDictionary<string, string>? metadata = null)
        {
            var mock = new Mock<IProjectTreeCustomizablePropertyContext>();
            mock.Setup(x => x.ItemName).Returns(itemName ?? string.Empty);
            mock.Setup<string?>(x => x.ItemType).Returns(itemType);
            mock.Setup(x => x.IsFolder).Returns(isFolder);
            mock.Setup(x => x.ParentNodeFlags).Returns(flags);
            mock.Setup(x => x.Metadata).Returns(metadata ?? ImmutableStringDictionary<string>.EmptyOrdinal);
            return mock.Object;
        }
    }
}
