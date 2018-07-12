// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    /// <summary>
    ///     Creates and handles releasing <see cref="AggregateCrossTargetProjectContext"/> instances based on the 
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
        /// <remarks>
        ///     When finished with the return <see cref="AggregateCrossTargetProjectContext"/>, callers must call 
        ///     <see cref="ReleaseProjectContextAsync(AggregateCrossTargetProjectContext)"/>.
        /// </remarks>
        Task<AggregateCrossTargetProjectContext> CreateProjectContextAsync();

        /// <summary>
        ///     Releases a previously created <see cref="AggregateCrossTargetProjectContext"/>.
        /// </summary>
        /// <param name="context">
        ///     The <see cref="AggregateCrossTargetProjectContext"/> to release.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="context"/> was not created via <see cref="CreateProjectContextAsync"/> or 
        ///     has already been unregistered.
        /// </exception>
        Task ReleaseProjectContextAsync(AggregateCrossTargetProjectContext context);
    }
}
