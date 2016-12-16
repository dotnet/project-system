// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// This service creates a file from a given file template.
    /// </summary>
    internal interface ICreateFileFromTemplateService
    {
        /// <summary>
        /// Create a file with the given template file and add it to the parent node.
        /// </summary>
        /// <param name="templateFile">The name of the template zip file.</param>
        /// <param name="parentDocumentMoniker">The path to the node to which the new file will be added.</param>
        /// <param name="fileName">The name of the file to be created.</param>
        /// <returns>true if file is added successfully.</returns>
        Task<bool> CreateFileAsync(string templateFile, string parentDocumentMoniker, string fileName);
    }
}
