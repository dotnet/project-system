// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
