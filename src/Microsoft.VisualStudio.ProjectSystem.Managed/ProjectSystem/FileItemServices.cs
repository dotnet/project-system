// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class FileItemServices
    {
        /// <summary>
        ///     Returns the logical folder names of the specified <paramref name="fullPath"/>, starting
        ///     at <paramref name="basePath"/>, using 'Link' metadata if it represents a linked file.
        /// </summary>
        public static string[]? GetLogicalFolderNames(string basePath, string fullPath, IImmutableDictionary<string, string> metadata)
        {
            Requires.NotNullOrEmpty(basePath, nameof(basePath));
            Requires.NotNullOrEmpty(fullPath, nameof(fullPath));
            Requires.NotNull(metadata, nameof(metadata));

            // Roslyn wants the effective set of folders from the source up to, but not including the project 
            // root to handle the cases where linked files have a different path in the tree than what its path 
            // on disk is. It uses these folders for code actions that create files alongside others, such as 
            // extract interface.

            string linkOrFullPath = GetLinkFilePath(metadata) ?? fullPath;

            // We try to make either the link path or full path relative to the project path
            string relativePath = PathHelper.MakeRelative(basePath, linkOrFullPath);
            if (relativePath.Length == 0)
                return null;

            // Is this outside of base path?
            if (Path.IsPathRooted(relativePath) || relativePath.StartsWith("..\\", StringComparisons.Paths))
                return null;

            string? relativeDirectoryName = Path.GetDirectoryName(relativePath);
            if (relativeDirectoryName?.Length == 0)
                return null;

            // We now have a folder in the form of `Folder1\Folder2` relative to the
            // project directory split it up into individual path components
            return relativeDirectoryName?.Split(Delimiter.Path);
        }

        private static string? GetLinkFilePath(IImmutableDictionary<string, string> metadata)
        {
            // This mimic's CPS's handling of Link metadata
            if (metadata.TryGetValue(Compile.LinkProperty, out string? linkFilePath) && !string.IsNullOrWhiteSpace(linkFilePath))
            {
                return linkFilePath.TrimEnd(Delimiter.Path);
            }

            return null;
        }
    }
}
