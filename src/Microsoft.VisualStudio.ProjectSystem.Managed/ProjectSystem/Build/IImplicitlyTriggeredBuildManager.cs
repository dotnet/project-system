// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    /// A MEF exportable interface whereby components may signal start/end of an implicitly triggered build
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IImplicitlyTriggeredBuildManager
    {
        /// <summary>
        /// Indicates start of an implicitly triggered build.
        /// </summary>
        void OnBuildStart();

        /// <summary>
        /// Indicates end or cancellation of an implicitly triggered build.
        /// </summary>
        void OnBuildEndOrCancel();
    }
}
