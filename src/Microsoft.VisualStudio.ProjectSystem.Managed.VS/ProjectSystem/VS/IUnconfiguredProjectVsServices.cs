// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to common Visual Studio project services provided by the <see cref="UnconfiguredProject"/>.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IUnconfiguredProjectVsServices : IUnconfiguredProjectCommonServices
    {
        /// <summary>
        ///     Gets <see cref="IVsHierarchy"/> provided by the <see cref="UnconfiguredProject"/>.
        /// </summary>
        IVsHierarchy VsHierarchy { get; }

        /// <summary>
        ///     Gets <see cref="IVsProject4"/> provided by the <see cref="UnconfiguredProject"/>.
        /// </summary>
        IVsProject4 VsProject { get; }

        /// <summary>
        ///     Gets <see cref="IPhysicalProjectTree"/> provided by the <see cref="UnconfiguredProject"/>.
        /// </summary>
        IPhysicalProjectTree ProjectTree { get; }
    }
}
