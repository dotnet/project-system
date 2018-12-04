// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal static class UnconfiguredProjectExtensions
    {
        public static string GetRelativePath(this UnconfiguredProject self, string path)
        {
            string projectFolder = Path.GetDirectoryName(self.FullPath);

            if (path.StartsWith(projectFolder, StringComparison.OrdinalIgnoreCase))
            {
                // Remove starting slashes
                int startingSlashCount = 0;
                while (path.Length > startingSlashCount && path[startingSlashCount] == '\\')
                {
                    startingSlashCount++;
                }

                path = path.Substring(startingSlashCount, projectFolder.Length - startingSlashCount);
            }

            return path;
        }
    }
}
