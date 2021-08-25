// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Transforms a collection of Visual Studio component IDs.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private)]
    internal interface IVisualStudioComponentIdTransformer
    {
        IReadOnlyCollection<string> TransformVisualStudioComponentIds(IEnumerable<string> vsComponentIds);
    }
}
