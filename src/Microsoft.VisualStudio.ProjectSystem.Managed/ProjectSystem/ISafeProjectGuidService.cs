// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS;

#pragma warning disable RS0030 // This is the one place where IProjectGuidService is allowed to be referenced

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides a mechanism to safely access the project GUID. Replaces usage of <see cref="IProjectGuidService"/>
    ///     and <see cref="IProjectGuidService2"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="IProjectGuidService"/> and <see cref="IProjectGuidService2"/> will retrieve the project GUID of
    ///     the project *at the time* that it is called. During project initialization, the GUID may be changed by the
    ///     solution in reaction to a clash with another project. <see cref="ISafeProjectGuidService"/> will wait until
    ///     it is safe to retrieve the project GUID before returning it.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ISafeProjectGuidService
    {
        /// <summary>
        ///     Returns the project GUID, waiting until project load has safely progressed
        ///     to a point where the GUID is guaranteed not to change.
        /// </summary>
        /// <returns>
        ///     The GUID of the current project.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        ///     The project was unloaded before project load had finished.
        /// </exception>
        Task<Guid> GetProjectGuidAsync();
    }
}
