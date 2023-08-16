// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate;

/// <summary>
/// Models the kinds of contributions Build Acceleration can make to a project build.
/// </summary>
internal enum BuildAccelerationResult
{
    /// <summary>
    /// Build Acceleration is disabled. A heuristic determined that the project
    /// would not have been accelerated anyway.
    /// </summary>
    DisabledNotCandidate,

    /// <summary>
    /// Build Acceleration is disabled. A heuristic determined that the project
    /// would have been accelerated, if the feature were turned on.
    /// </summary>
    DisabledCandidate,

    /// <summary>
    /// Build Acceleration is enabled, but the project was not accelerated.
    /// </summary>
    EnabledNotAccelerated,

    /// <summary>
    /// Build Acceleration is enabled, and the project was accelerated.
    /// </summary>
    EnabledAccelerated
}
