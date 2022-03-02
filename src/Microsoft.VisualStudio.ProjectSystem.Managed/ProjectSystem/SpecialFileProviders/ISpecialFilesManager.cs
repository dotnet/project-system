// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Queries the project for special files, such as the application config file or application designer folder, and optionally creates them and checks them out from source control.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ISpecialFilesManager
    {
        /// <summary>
        ///     Returns the path to a special file, optionally creating it and
        ///     checking it out from source control.
        /// </summary>
        /// <param name="fileId">
        ///     One of the <see cref="SpecialFiles"/> values indicating the special file to return.
        /// </param>
        /// <param name="flags">
        ///     One or more of the <see cref="SpecialFileFlags"/>
        /// </param>
        /// <returns>
        ///     The file name of the special file, or <see langword="null"/> if special file is not
        ///     handled by the project.
        /// </returns>
        Task<string?> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags);
    }
}
