// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Provides a set of helper methods for interacting with designated start up projects.
    /// </summary>
    internal interface IStartupProjectHelper
    {
        /// <summary>
        /// Provides a mechanism to get an export from DotNet projects designated as startup projects. The <paramref name="capabilityMatch"/> is
        /// used to refine the projects that are considered.
        /// </summary>
        ImmutableArray<T> GetExportFromDotNetStartupProjects<T>(string capabilityMatch) where T : class;

        /// <summary>
        /// Retrieves the full paths to the designated startup projects.
        /// </summary>
        ImmutableArray<string> GetFullPathsOfStartupProjects();
    }
}
