// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;
using static Microsoft.VisualStudio.VSConstants;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Extension methods for <see cref="IProjectService"/>.
    /// </summary>
    internal static class IProjectServiceExtensions
    {
        /// <summary>
        /// Obtains the <see cref="UnconfiguredProject"/> for <paramref name="vsHierarchy"/>,
        /// optionally testing whether it has a capability match to <paramref name="appliesToExpression"/>.
        /// </summary>
        /// <param name="projectService">The project service to invoke this method on.</param>
        /// <param name="vsHierarchy">The VS hierarchy object, which must represent a CPS project.</param>
        /// <param name="appliesToExpression">An optional project capability match expression to filter out non-matching projects.</param>
        /// <returns>
        /// The CPS <see cref="UnconfiguredProject"/> for the given hierarchy node, matching any (optional)
        /// capability expression, or <see langword="null"/> if no suitable project exists.
        /// </returns>
        public static UnconfiguredProject? GetUnconfiguredProject(this IProjectService projectService, IVsHierarchy vsHierarchy, string? appliesToExpression = null)
        {
            // We need IProjectService2.GetLoadedProject.
            if (projectService is IProjectService2 projectService2)
            {
                vsHierarchy.GetCanonicalName((uint)VSITEMID.Root, out string? projectFilePath);

                // Ensure we have a path.
                if (!Strings.IsNullOrEmpty(projectFilePath))
                {
                    UnconfiguredProject? unconfiguredProject = projectService2.GetLoadedProject(projectFilePath);

                    // Ensure CPS knows a project having that path.
                    if (unconfiguredProject is not null)
                    {
                        // If capabilites were requested, match them.
                        if (appliesToExpression is null || unconfiguredProject.Capabilities.AppliesTo(appliesToExpression))
                        {
                            // Found a matching project.
                            return unconfiguredProject;
                        }
                    }
                }
            }

            return null;
        }
    }
}
