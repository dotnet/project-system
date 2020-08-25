// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Logging
{
    /// <summary>
    ///     Provides access to the project output window pane.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IProjectOutputWindowPaneProvider
    {
        /// <summary>
        ///     Returns the project output window pane.
        /// </summary>
        /// <returns>
        ///     The project <see cref="IVsOutputWindowPane"/> object, or <see langword="null"/>
        ///     if the <see cref="IVsOutputWindow"/> service is not present.
        /// </returns>
        Task<IVsOutputWindowPane?> GetOutputWindowPaneAsync();
    }
}
