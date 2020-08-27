// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.IO
{
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IFileExplorer
    {
        /// <summary>
        ///     Opens the containing folder of the specified path in File Explorer, selecting the file if it exists.
        /// </summary>
        void OpenContainingFolder(string path);

        /// <summary>
        ///     Opens the contents of the specified folder in File Explorer.
        /// </summary>
        void OpenFolder(string path);
    }
}
