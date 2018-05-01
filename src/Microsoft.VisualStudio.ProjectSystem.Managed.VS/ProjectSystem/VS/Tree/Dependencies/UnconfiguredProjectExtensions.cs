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
                path = path.Substring(projectFolder.Length).TrimStart('\\');
            }

            return path;
        }
    }
}
