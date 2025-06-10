// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

/// <summary>
/// Models the set of launch profiles and global settings defined in a project.
/// </summary>
/// <remarks>
/// Can be thought of as the object model for a <c>launchSettings.json</c> file, with the additional
/// concept of an active profile.
///
/// Implementations of this interface are expected to be immutable.
/// </remarks>
public interface ILaunchSettings
{
    /// <summary>
    /// Gets the currently active launch profile for the project.
    /// </summary>
    /// <remarks>
    /// If an active profile has not been specified, defaults to the first profile in <see cref="Profiles"/>.
    ///
    /// Will be <see langword="null"/> if <see cref="Profiles"/> is empty.
    /// </remarks>
    ILaunchProfile? ActiveProfile { get; }

    /// <summary>
    /// Gets the list of all launch profiles provided by this project.
    /// </summary>
    ImmutableList<ILaunchProfile> Profiles { get; }

    /// <summary>
    /// Provides access to custom global launch settings data. The returned value depends
    /// on the section being retrieved. <paramref name="settingsName"/> matches the section
    /// in the settings file.
    /// </summary>
    /// <remarks>
    /// This method just performs a lookup on <see cref="GlobalSettings"/>.
    /// </remarks>
    object? GetGlobalSetting(string settingsName);

    /// <summary>
    /// Gets a dictionary of global settings.
    /// </summary>
    ImmutableDictionary<string, object> GlobalSettings { get; }
}
