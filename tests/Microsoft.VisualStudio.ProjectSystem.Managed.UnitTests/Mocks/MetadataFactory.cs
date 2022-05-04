// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class MetadataFactory
    {
        public static IImmutableDictionary<string, IImmutableDictionary<string, string>> Create(string fileName, (string name, string value) metadata)
        {
            return ImmutableStringDictionary<IImmutableDictionary<string, string>>.EmptyOrdinal.Add(fileName,
                                                                                                    ImmutableStringDictionary<string>.EmptyOrdinalIgnoreCase.Add(metadata.name, metadata.value));
        }

        public static IImmutableDictionary<string, IImmutableDictionary<string, string>> Add(
            this IImmutableDictionary<string, IImmutableDictionary<string, string>> metadata,
            string fileName,
            (string name, string value) itemMetadata)
        {
            return metadata.Add(fileName,
                                ImmutableStringDictionary<string>.EmptyOrdinalIgnoreCase.Add(itemMetadata.name, itemMetadata.value));
        }
    }
}
