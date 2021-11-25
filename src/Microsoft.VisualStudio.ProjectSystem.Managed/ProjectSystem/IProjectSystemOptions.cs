// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides properties for retrieving options for the project system.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IProjectSystemOptions
    {
        /// <summary>
        ///     Gets a value indicating if the project fast up to date check is enabled.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A token whose cancellation signals lost interest in the result.
        /// </param>
        /// <value>
        ///     <see langword="true"/> if the project fast up to date check is enabled; otherwise, <see langword="false"/>
        /// </value>
        Task<bool> GetIsFastUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets a value indicating the level of fast up to date check logging.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A token whose cancellation signals lost interest in the result.
        /// </param>
        /// <value>
        ///     The level of fast up to date check logging.
        /// </value>
        Task<LogLevel> GetFastUpToDateLoggingLevelAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets a value indicating whether the designer view is the default editor for the specified designer category.
        /// </summary>
        Task<bool> GetUseDesignerByDefaultAsync(string designerCategory, bool defaultValue, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Sets a value indicating whether the designer view is the default editor for the specified designer category.
        /// </summary>
        Task SetUseDesignerByDefaultAsync(string designerCategory, bool value, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets a value indicating if analyzers should be skipped for implicitly triggered build.
        /// </summary>
        Task<bool> GetSkipAnalyzersForImplicitlyTriggeredBuildAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets a value indicating if single-target builds should be preferred for startup projects. 
        /// </summary>
        Task<bool> GetPreferSingleTargetBuildsForStartupProjectsAsync(CancellationToken cancellationToken = default);
    }
}
