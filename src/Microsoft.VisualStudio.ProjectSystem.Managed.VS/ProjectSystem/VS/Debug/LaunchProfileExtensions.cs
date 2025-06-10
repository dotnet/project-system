// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

internal static class LaunchProfileExtensions
{
    /// <summary>
    /// Gets whether the profile has the "Project" command name (launches the project's output).
    /// </summary>
    public static bool IsRunProjectCommand(this ILaunchProfile profile)
    {
        return string.Equals(profile.CommandName, LaunchSettingsProvider.RunProjectCommandName, StringComparisons.LaunchProfileCommandNames);
    }

    /// <summary>
    /// Gets whether the profile has the "Executable" command name (launches the an arbitrary executable).
    /// </summary>
    public static bool IsRunExecutableCommand(this ILaunchProfile profile)
    {
        return string.Equals(profile.CommandName, LaunchSettingsProvider.RunExecutableCommandName, StringComparisons.LaunchProfileCommandNames);
    }
}
