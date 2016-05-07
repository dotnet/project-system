// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides extension methods for <see cref="IProjectTree"/> instances.
    /// </summary>
    internal static class ProjectTreeExtensions
    {
        /// <summary>
        ///     Returns a value indicating whether the specified <see cref="IProjectTree"/> is
        ///     the project root; that is, has the flag <see cref="ProjectTreeFlags.Common.ProjectRoot"/>.
        /// </summary>
        public static bool IsProjectRoot(this IProjectTree node)
        {
            Requires.NotNull(node, nameof(node));

            return node.HasFlag(ProjectTreeFlags.Common.ProjectRoot);
        }

        /// <summary>
        ///     Returns a value indicating whether the specified <see cref="IProjectTree"/> has
        ///     the specified flag.
        /// </summary>
        public static bool HasFlag(this IProjectTree node, ProjectTreeFlags.Common flag)
        {
            Requires.NotNull(node, nameof(node));

            return node.Flags.Contains(flag); 
        }

        /// <summary>
        ///     Returns a value indicating whether the specified <see cref="IProjectTree"/> is 
        ///     included as part of the project.
        /// </summary>
        public static bool IsIncludedInProject(this IProjectTree node)
        {
            Requires.NotNull(node, nameof(node));

            return !node.HasFlag(ProjectTreeFlags.Common.IncludeInProjectCandidate);
        }
    }
}
