// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Represents the data source of metadata needed for input into restore operations for an individual <see cref="ConfiguredProject"/>.
    ///     This will be later combined with other implicitly active <see cref="ConfiguredProject"/> instances within a
    ///     <see cref="UnconfiguredProject"/> to provide enough data to restore the entire project.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IPackageRestoreConfiguredInputDataSource : IProjectValueDataSource<PackageRestoreConfiguredInput>
    {
    }
}
