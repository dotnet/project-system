// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// This service creates a file from a given file template.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ICreateFileFromTemplateService
    {
        /// <summary>
        /// Create a file with the given template file and add it to the parent node.
        /// </summary>
        /// <param name="templateFile">The name of the template zip file.</param>
        /// <param name="path">The path to the file to be created.</param>
        /// <returns>true if file is added successfully.</returns>
        Task<bool> CreateFileAsync(string templateFile, string path);
    }
}
