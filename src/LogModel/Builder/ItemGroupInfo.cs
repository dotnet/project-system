// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class ItemGroupInfo
    {
        public string Name { get; }
        public ImmutableList<ItemInfo> Items { get; }

        public ItemGroupInfo(string name, ImmutableList<ItemInfo> items)
        {
            Name = name;
            Items = items;
        }
    }
}
