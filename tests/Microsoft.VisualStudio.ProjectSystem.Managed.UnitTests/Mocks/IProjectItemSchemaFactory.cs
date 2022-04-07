// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
