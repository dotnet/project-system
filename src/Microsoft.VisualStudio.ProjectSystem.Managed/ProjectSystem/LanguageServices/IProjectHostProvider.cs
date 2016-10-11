// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Creates the VS specific project host (IVSHierarchy) for projects.
    /// </summary>
    internal interface IProjectHostProvider
    {
        /// <summary>
        /// Creates an <see cref="IConfiguredProjectHostObject"/> for the configured project with the given display name and unconfigured project host object.
        /// </summary>
        /// <param name="unconfiguredProjectHostObject">Host object for the underlying unconfigured project.</param>
        /// <param name="projectDisplayName">Project display name for the configured cross-targeting project.</param>
        IConfiguredProjectHostObject GetConfiguredProjectHostObject(IUnconfiguredProjectHostObject unconfiguredProjectHostObject, string projectDisplayName);

        /// <summary>
        /// Creates an <see cref="IUnconfiguredProjectHostObject"/> for the given unconfigured project.
        /// </summary>
        /// <param name="project">Unconfigured project.</param>
        IUnconfiguredProjectHostObject GetUnconfiguredProjectHostObject(UnconfiguredProject project);
    }
}
