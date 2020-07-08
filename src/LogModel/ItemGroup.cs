// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class ItemGroup
    {
        public string Name { get; }
        public ImmutableList<Item> Items { get; }

        public ItemGroup(string name, ImmutableList<Item> items)
        {
            Name = name;
            Items = items;
        }
    }
}
