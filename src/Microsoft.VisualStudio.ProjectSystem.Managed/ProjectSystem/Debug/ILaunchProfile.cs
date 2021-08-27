// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Interface definition for a profile
    /// </summary>
    public interface ILaunchProfile
    {
        string? Name { get; }
        string? CommandName { get; }
        string? ExecutablePath { get; }
        string? CommandLineArgs { get; }
        string? WorkingDirectory { get; }
        bool LaunchBrowser { get; }
        string? LaunchUrl { get; }
        ImmutableDictionary<string, string>? EnvironmentVariables { get; }
        ImmutableDictionary<string, object>? OtherSettings { get; }
    }
}
