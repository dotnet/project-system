// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Runtimes
{
    /// <summary>
    /// Project value data source for instances of <see cref="RuntimeDescriptor"/>.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IRuntimeDescriptorDataSource : IProjectValueDataSource<ISet<RuntimeDescriptor>>
    {
    }
}
