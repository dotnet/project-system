// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Represents the data source of metadata needed for input into restore operations for a <see cref="UnconfiguredProject"/>
    ///     instance by resolving conflicts and combining the data of all implicitly active <see cref="ConfiguredProject"/>
    ///     instances.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IPackageRestoreUnconfiguredInputDataSource : IProjectValueDataSource<PackageRestoreUnconfiguredInput>
    {
    }
}
