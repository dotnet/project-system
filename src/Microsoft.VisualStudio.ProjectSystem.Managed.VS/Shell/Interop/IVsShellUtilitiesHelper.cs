// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Shell.Interop
{
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IVsShellUtilitiesHelper
    {
        /// <summary>
        /// Determines whether Visual Studio was installed from a preview channel.
        /// </summary>
        Task<bool> IsVSFromPreviewChannelAsync();
    }
}
