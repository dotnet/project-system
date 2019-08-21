// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.VisualStudio.Utilities
{
    internal static class RepoUtil
    {
        /// <summary>
        /// Gets the absolute path to the checked out location of this repo.
        /// </summary>
        /// <remarks>
        /// Intended for unit tests that need to inspect files in the repo itself.
        /// </remarks>
        public static string FindRepoRootPath()
        {
            // Start with this DLL's location
            string path = typeof(RepoUtil).Assembly.Location;

            // Walk up the tree until we find the 'artifacts' folder
            while (!Path.GetFileName(path).Equals("artifacts", StringComparisons.Paths))
            {
                path = Path.GetDirectoryName(path);
            }

            // Go up one more level
            path = Path.GetDirectoryName(path);
            return path;
        }
    }
}
