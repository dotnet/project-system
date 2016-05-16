// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


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
        ///     the node is a folder.
        /// </summary>
        public static bool IsFolder(this ProjectTreeFlags flags)
        {
            return flags.HasFlag(ProjectTreeFlags.Common.Folder);
        }

        /// <summary>
        ///     Returns a value indicating whether the specified flags exist.
        /// </summary>
        public static bool Contains(this ProjectTreeFlags source, ProjectTreeFlags flags)
        {
            var newFlags = source.Except(flags);

            return ((source.Count - newFlags.Count)) == flags.Count;
        }
    }
}
