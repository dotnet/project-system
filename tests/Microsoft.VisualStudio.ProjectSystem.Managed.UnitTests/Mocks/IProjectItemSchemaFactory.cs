// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectItemSchemaFactory
    {
        public static IProjectItemSchema Create(IImmutableList<IItemType> itemTypes)
        {
            var projectItemSchema = new Mock<IProjectItemSchema>();

            projectItemSchema.Setup(o => o.GetKnownItemTypes())
                .Returns(() => ImmutableHashSet<string>.Empty.Union(itemTypes.Select(i => i.Name)));

            projectItemSchema.Setup(o => o.GetItemType(It.IsAny<string>()))
                .Returns((string itemTypeName) => itemTypes.FirstOrDefault(i => i.Name == itemTypeName));

            return projectItemSchema.Object;
        }
    }
}
