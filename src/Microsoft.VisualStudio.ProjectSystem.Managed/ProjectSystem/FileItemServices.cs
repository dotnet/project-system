// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class FileItemServices
    {
        public static readonly char[] PathSeparatorCharacters = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        public static string GetLinkFilePath(IImmutableDictionary<string, string> metadata)
        {
            Requires.NotNull(metadata, nameof(metadata));

            // This mimic's CPS's handling of Link metadata
            if (metadata.TryGetValue(ConfigurationGeneralFile.LinkProperty, out string linkFilePath) && !string.IsNullOrWhiteSpace(linkFilePath))
            {
                return linkFilePath.TrimEnd(PathSeparatorCharacters);
            }

            return null;
        }
    }
}
