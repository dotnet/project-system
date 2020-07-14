// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
