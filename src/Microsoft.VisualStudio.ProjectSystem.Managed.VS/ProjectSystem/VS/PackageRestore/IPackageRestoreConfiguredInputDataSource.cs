// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
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
