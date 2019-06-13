// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides access to common project services provided by the <see cref="UnconfiguredProject"/>.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IUnconfiguredProjectCommonServices
    {
        /// <summary>
        ///     Gets the <see cref="IProjectThreadingService"/> for the current <see cref="UnconfiguredProject"/>.
        /// </summary>
        IProjectThreadingService ThreadingService
        {
            get;
        }

        /// <summary>
        ///     Gets the current <see cref="UnconfiguredProject"/>.
        /// </summary>
        UnconfiguredProject Project
        {
            get;
        }

        /// <summary>
        ///     Gets the current active <see cref="ConfiguredProject"/>.
        /// </summary>
        ConfiguredProject ActiveConfiguredProject
        {
            get;
        }

        /// <summary>
        ///     Gets the <see cref="ProjectProperties"/> of the currently active configured project.
        /// </summary>
        ProjectProperties ActiveConfiguredProjectProperties
        {
            get;
        }

        /// <summary>
        ///     Gets the <see cref="IProjectAccessor"/> which provides access to MSBuild evaluation and construction models for a project.
        /// </summary>
        IProjectAccessor ProjectAccessor
        {
            get;
        }
    }
}
