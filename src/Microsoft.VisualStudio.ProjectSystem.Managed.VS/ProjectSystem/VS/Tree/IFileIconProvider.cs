// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree
{
    /// <summary>
    /// Provides icons for files based on their file names.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne, Provider = ProjectSystemContractProvider.Private)]
    public interface IFileIconProvider
    {
        /// <summary>
        /// Gets the icon associated with <paramref name="path"/>'s extension.
        /// </summary>
        /// <remarks>
        /// If no specific icon could be determined, <see cref="KnownMonikers.Document"/> is returned.
        /// </remarks>
        ImageMoniker GetFileExtensionImageMoniker(string path);
    }
}
