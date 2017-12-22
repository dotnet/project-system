// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides properties and events to track the implicit activation of a <see cref="ConfiguredProject"/>.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IConfiguredProjectImplicitActivationTracking
    {
        /// <summary>
        ///     Occurs when the current <see cref="ConfiguredProject"/> becomes implicitly active.
        /// </summary>
        event AsyncEventHandler ImplicitlyActivated;

        /// <summary>
        ///     Occurs when the current <see cref="ConfiguredProject"/> is no longer implicitly active.
        /// </summary>
        event AsyncEventHandler ImplicitlyDeactivated;

        /// <summary>
        ///     Gets a value indicating whether the current <see cref="ConfiguredProject"/> 
        ///     is implicitly active.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="IConfiguredProjectImplicitActivationTracking"/> has been disposed of.
        /// </exception>
        bool IsImplicitlyActive
        {
            get;
        }

        /// <summary>
        ///     Gets a task that is completed when current <see cref="ConfiguredProject"/> becomes 
        ///     implicitly active.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="IConfiguredProjectImplicitActivationTracking"/> has been disposed of.
        /// </exception>
        /// <remarks>
        ///     The returned <see cref="Task"/> is canceled when the <see cref="ConfiguredProject"/> 
        ///     is unloaded.
        /// </remarks>
        Task IsImplicitlyActiveTask
        {
            get;
        }
    }
}
