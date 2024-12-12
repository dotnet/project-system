﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Utilities;

internal static class RepoUtil
{
    private static string? _root;

    /// <summary>
    /// Gets the absolute path to the checked out location of this repo.
    /// </summary>
    /// <remarks>
    /// Intended for unit tests that need to inspect files in the repo itself.
    /// </remarks>
    public static string FindRepoRootPath()
    {
        if (_root is null)
        {
            // Start with this DLL's location
            string path = typeof(RepoUtil).Assembly.Location;

            // Walk up the tree until we find the 'artifacts' folder
            while (!Path.GetFileName(path).Equals("artifacts", StringComparisons.Paths))
            {
                path = Path.GetDirectoryName(path);
            }

            // Go up one more level
            _root = Path.GetDirectoryName(path);
        }

        return _root;
    }
}
