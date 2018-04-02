// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class ItemInfo
    {
        public string Name { get; }
        public ImmutableDictionary<string, string> Metadata { get; }

        public ItemInfo(string name)
        {
            Name = name;
            Metadata = ImmutableDictionary<string, string>.Empty;
        }

        public ItemInfo(string name, IEnumerable<KeyValuePair<string, string>> metadata)
        {
            Name = name;
            var tempMetadata = new Dictionary<string, string>();
            if (metadata != null)
            {
                foreach (var pair in metadata)
                {
                    AddMetadata(tempMetadata, pair.Key, pair.Value);
                }
            }
            Metadata = tempMetadata.ToImmutableDictionary();
        }

        public void AddMetadata(Dictionary<string, string> metadata, string key, string value) => 
            metadata[key] = metadata.ContainsKey(key)
                ? throw new LoggerException(Resources.OverwritingMetadata)
                : value;
    }
}
