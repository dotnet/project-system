// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides methods for creating and releasing <see cref="IWorkspaceProjectContextAccessor"/> instances.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IWorkspaceProjectContextProvider
    {
        /// <summary>
        ///     Creates a <see cref="IWorkspaceProjectContextAccessor"/> for the specified <see cref="ConfiguredProject"/>.
        /// </summary>
        /// <returns>
        ///     The created <see cref="IWorkspaceProjectContextAccessor"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="project"/> is <see langword="null"/>.
        /// </exception>
        Task<IWorkspaceProjectContextAccessor?> CreateProjectContextAsync(ConfiguredProject project);

        /// <summary>
        ///     Release the <see cref="IWorkspaceProjectContextAccessor"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="accessor"/> is <see langword="null"/>.
        /// </exception>
        Task ReleaseProjectContextAsync(IWorkspaceProjectContextAccessor accessor);
    }
}
