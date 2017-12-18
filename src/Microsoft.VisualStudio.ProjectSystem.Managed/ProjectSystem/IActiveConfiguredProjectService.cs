// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides properties and events to track activation of an <see cref="ConfiguredProject"/>.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IActiveConfiguredProjectService
    {
        /// <summary>
        ///     Gets a value indicating whether the configuration group containing the 
        ///     current <see cref="ConfiguredProject"/> is active.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="IActiveConfigurationGroupService"/> has been disposed of.
        /// </exception>
        bool IsActive
        {
            get;
        }

        /// <summary>
        ///     Gets a task that is completed when the configuration group containing the
        ///     current <see cref="ConfiguredProject"/> becomes active.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="IActiveConfigurationGroupService"/> has been disposed of.
        /// </exception>
        /// <remarks>
        ///     The returned <see cref="Task"/> is cancelled when the 
        ///     <see cref="ConfiguredProject"/> is unloaded.
        /// </remarks>
        Task IsActiveTask
        {
            get;
        }
    }
}
