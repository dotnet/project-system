// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Item
    {
        public string Name { get; }

        public ImmutableDictionary<string, string> Metadata { get; }

        public Item(string name, ImmutableDictionary<string, string> metadata)
        {
            Name = name;
            Metadata = metadata;
        }
    }
}
