// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS;

/// <summary>
///     Provides feature flag names used throughout this project.
/// </summary>
internal static class FeatureFlagNames
{
    /// <summary>
    /// Enables NuGet restore to detect cycles of type A -> B -> A to avoid loops.
    /// </summary>
    public const string EnableNuGetRestoreCycleDetection = "ManagedProjectSystem.EnableNuGetRestoreCycleDetection";

    /// <summary>
    /// Enables logs in incremental builds.
    /// </summary>
    public const string EnableIncrementalBuildFailureOutputLogging = "ManagedProjectSystem.EnableIncrementalBuildFailureOutputLogging";

    /// <summary>
    /// Enables incremental build to report build failures.
    /// </summary>
    public const string EnableIncrementalBuildFailureTelemetry = "ManagedProjectSystem.EnableIncrementalBuildFailureTelemetry";

    /// <summary>
    /// Controls whether Build Acceleration should be enabled when a project does not explicitly opt in
    /// or out via the <c>AccelerateBuildsInVisualStudio</c> MSBuild property.
    /// </summary>
    public const string EnableBuildAccelerationByDefault = "ManagedProjectSystem.EnableBuildAccelerationByDefault";

    /// <summary>
    /// When this feature flag is enabled, build diagnostics will be published by CPS and should not be passed to Roslyn.
    /// </summary>
    public const string LspPullDiagnosticsFeatureFlagName = "Lsp.PullDiagnostics";
}
