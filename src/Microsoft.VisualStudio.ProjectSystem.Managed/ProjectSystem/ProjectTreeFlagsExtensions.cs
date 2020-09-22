// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides extension methods for <see cref="ProjectTreeFlags"/>.
    /// </summary>
    internal static class ProjectTreeFlagsExtensions
    {
        /// <summary>
        ///     Returns a value indicating whether the specified flags has the flag <see cref="ProjectTreeFlags.Common.ProjectRoot"/>.
        /// </summary>
        public static bool IsProjectRoot(this ProjectTreeFlags flags)
        {
            return flags.HasFlag(ProjectTreeFlags.Common.ProjectRoot);
        }

        /// <summary>
        ///     Returns a value indicating whether the specified flags has the specified flag.
        /// </summary>
        public static bool HasFlag(this ProjectTreeFlags flags, ProjectTreeFlags.Common flag)
        {
            return flags.Contains(flag);
        }

        /// <summary>
        ///     Returns a value indicating whether the specified flags indicates that
        ///     the node is included as part of the project.
        /// </summary>
        public static bool IsIncludedInProject(this ProjectTreeFlags flags)
        {
            return !flags.HasFlag(ProjectTreeFlags.Common.IncludeInProjectCandidate);
        }

        /// <summary>
        ///     Returns a value indicating whether the specified flags indicates that
        ///     the file or folder is missing on disk.
        /// </summary>
        public static bool IsMissingOnDisk(this ProjectTreeFlags flags)
        {
            return !flags.HasFlag(ProjectTreeFlags.Common.FileSystemEntity);
        }

        /// <summary>
        ///     Returns a value indicating whether the specified flags indicates that
        ///     the node is a folder.
        /// </summary>
        public static bool IsFolder(this ProjectTreeFlags flags)
        {
            return flags.HasFlag(ProjectTreeFlags.Common.Folder);
        }
    }
}
