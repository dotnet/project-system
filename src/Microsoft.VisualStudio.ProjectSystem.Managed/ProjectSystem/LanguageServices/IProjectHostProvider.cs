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
        /// <param name="workspaceProjectContextId">The unique identifier of the <see cref="ConfiguredProject"/> instance's <see cref="IWorkspaceProjectContext"/>.</param>
        /// <param name="targetFramework">Target framework for the configured cross-targeting project.</param>
        IConfiguredProjectHostObject GetConfiguredProjectHostObject(IUnconfiguredProjectHostObject unconfiguredProjectHostObject, string workspaceProjectContextId, string targetFramework);

        /// <summary>
        /// Gets an <see cref="IUnconfiguredProjectHostObject"/> for the current unconfigured project.
        /// </summary>
        IUnconfiguredProjectHostObject UnconfiguredProjectHostObject { get; }
    }
}
