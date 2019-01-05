// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    /// <summary>
    ///     Creates <see cref="AggregateCrossTargetProjectContext"/> instances based on the 
    ///     current <see cref="UnconfiguredProject"/>.
    /// </summary>
    internal interface IAggregateCrossTargetProjectContextProvider
    {
        /// <summary>
        ///     Creates a <see cref="AggregateCrossTargetProjectContext"/>.
        /// </summary>
        /// <returns>
        ///     The created <see cref="AggregateCrossTargetProjectContext"/> or <see langword="null"/> if it could
        ///     not be created due to the project targeting an unrecognized language.
        /// </returns>
        Task<AggregateCrossTargetProjectContext> CreateProjectContextAsync();
    }
}
