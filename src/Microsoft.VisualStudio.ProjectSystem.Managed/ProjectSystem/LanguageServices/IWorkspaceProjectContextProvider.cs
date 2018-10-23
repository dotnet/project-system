// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides methods for creating and releasing <see cref="IWorkspaceProjectContext"/> instances.
    /// </summary>
    internal interface IWorkspaceProjectContextProvider
    {
        /// <summary>
        ///     Creates a <see cref="IWorkspaceProjectContext"/> for the specified <see cref="ConfiguredProject"/>.
        /// </summary>
        /// <returns>
        ///     The created <see cref="IWorkspaceProjectContext"/>; otherwise, <see langword="null"/> if the context
        ///     could not be created due to missing data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="project"/> is <see langword="null"/>.
        /// </exception>
        Task<IWorkspaceProjectContext> CreateProjectContextAsync(ConfiguredProject project);

        /// <summary>
        ///     Release the <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="projectContext"/> is <see langword="null"/>.
        /// </exception>
        Task ReleaseProjectContextAsync(IWorkspaceProjectContext projectContext);
    }
}
