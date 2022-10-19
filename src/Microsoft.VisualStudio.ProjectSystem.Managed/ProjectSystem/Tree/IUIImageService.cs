// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tree;

[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne, Provider = ProjectSystemContractProvider.Private)]
internal interface IUIImageService
{
    /// <summary>
    /// Returns an image moniker based on the file type.
    /// </summary>
    /// <remarks>
    /// This requires the main thread.
    /// </remarks>
    /// <param name="filename"></param>
    /// <returns></returns>
    ImageMoniker GetImageMonikerForFile(string filename);
}
