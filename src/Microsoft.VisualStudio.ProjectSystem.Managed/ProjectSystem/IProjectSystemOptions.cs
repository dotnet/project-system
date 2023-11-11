// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        Task<bool> GetIsFastUpToDateCheckEnabledAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Gets a value indicating the level of fast up to date check logging.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A token whose cancellation signals lost interest in the result.
        /// </param>
        /// <value>
        ///     The level of fast up to date check logging.
        /// </value>
        Task<LogLevel> GetFastUpToDateLoggingLevelAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Gets a value indicating whether the designer view is the default editor for the specified designer category.
        /// </summary>
        Task<bool> GetUseDesignerByDefaultAsync(string designerCategory, bool defaultValue, CancellationToken cancellationToken);

        /// <summary>
        ///     Sets a value indicating whether the designer view is the default editor for the specified designer category.
        /// </summary>
        Task SetUseDesignerByDefaultAsync(string designerCategory, bool value, CancellationToken cancellationToken);

        /// <summary>
        ///     Gets a value indicating if analyzers should be skipped for implicitly triggered build.
        /// </summary>
        Task<bool> GetSkipAnalyzersForImplicitlyTriggeredBuildAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Gets a value indicating if single-target builds should be preferred for startup projects.
        /// </summary>
        Task<bool> GetPreferSingleTargetBuildsForStartupProjectsAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Gets whether incremental build failure detection should write to the output window when failures are detected.
        /// </summary>
        ValueTask<bool> IsIncrementalBuildFailureOutputLoggingEnabledAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Gets whether incremental build failure detection should send telemetry.
        /// </summary>
        ValueTask<bool> IsIncrementalBuildFailureTelemetryEnabledAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Gets whether Build Acceleration should be enabled when a project does not explicitly opt in
        ///     or out via the <c>AccelerateBuildsInVisualStudio</c> MSBuild property.
        /// </summary>
        ValueTask<bool> IsBuildAccelerationEnabledByDefaultAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Gets whether LSP pull diagnostics are enabled.
        /// </summary>
        ValueTask<bool> IsLspPullDiagnosticsEnabledAsync(CancellationToken cancellationToken);
    }
}
