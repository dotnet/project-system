// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Provides a view of the project's restore state.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IPackageRestoreService
    {
        /// <summary>
        ///     Gets the block that broadcasts current restore data.
        /// </summary>
        IReceivableSourceBlock<IProjectVersionedValue<RestoreData>> RestoreData
        {
            get;
        }
    }
}
