// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class MetadataFactory
    {
        public static IImmutableDictionary<string, IImmutableDictionary<string, string>> Create(string fileName, (string name, string value) metadata)
        {
            return ImmutableStringDictionary<IImmutableDictionary<string, string>>.EmptyOrdinal.Add(fileName,
                                                                                                    ImmutableStringDictionary<string>.EmptyOrdinalIgnoreCase.Add(metadata.name, metadata.value));
        }
    }
}
