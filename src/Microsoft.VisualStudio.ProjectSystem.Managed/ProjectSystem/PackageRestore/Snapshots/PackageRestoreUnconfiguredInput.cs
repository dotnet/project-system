// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

/// <summary>
///     Represents restore input data for an <see cref="UnconfiguredProject"/>.
/// </summary>
internal sealed class PackageRestoreUnconfiguredInput(
    ProjectRestoreInfo? restoreInfo,
    IReadOnlyCollection<PackageRestoreConfiguredInput> configuredInputs,
    ProjectConfiguration activeConfiguration)
{
    /// <summary>
    ///     Gets the restore information produced in this input. Can be <see langword="null"/> if
    ///     the project has no active configurations.
    /// </summary>
    public ProjectRestoreInfo? RestoreInfo { get; } = restoreInfo;

    /// <summary>
    ///     Gets the <see cref="PackageRestoreConfiguredInput"/> instances that contributed to <see cref="RestoreInfo"/>.
    /// </summary>
    public IReadOnlyCollection<PackageRestoreConfiguredInput> ConfiguredInputs { get; } = configuredInputs;

    /// <summary>
    ///     Gets the active configuration.
    /// </summary>
    public ProjectConfiguration ActiveConfiguration { get; } = activeConfiguration;
}
