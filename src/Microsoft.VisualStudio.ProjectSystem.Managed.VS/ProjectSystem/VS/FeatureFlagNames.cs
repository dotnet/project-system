// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS;

/// <summary>
///     Provides feature flag names used throughout this project.
/// </summary>
internal static class FeatureFlagNames
{
    // Use this prefix name for all feature flags where the feature implementations belongs to dotnet project system.
    public const string Prefix = "ManagedProjectSystem";

    /// <summary>
    /// Enables NuGet restore to detect cycles of type A -> B -> A to avoid loops.
    /// </summary>
    public const string EnableNuGetRestoreCycleDetection = Prefix + ".EnableNuGetRestoreCycleDetection";

    /// <summary>
    /// Enables logs in incremental builds.
    /// </summary>
    public const string EnableIncrementalBuildFailureOutputLogging = Prefix + ".EnableIncrementalBuildFailureOutputLogging";

    /// <summary>
    /// Enables incremental build to report build failures.
    /// </summary>
    public const string EnableIncrementalBuildFailureTelemetry = Prefix + ".EnableIncrementalBuildFailureTelemetry";
}
