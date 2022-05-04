// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Interface definition for a writable launch profile
    /// </summary>
    public interface IWritableLaunchProfile
    {
        string? Name { get; set; }
        string? CommandName { get; set; }
        string? ExecutablePath { get; set; }
        string? CommandLineArgs { get; set; }
        string? WorkingDirectory { get; set; }
        bool LaunchBrowser { get; set; }
        string? LaunchUrl { get; set; }
        Dictionary<string, string> EnvironmentVariables { get; }
        Dictionary<string, object> OtherSettings { get; }

        /// <summary>
        /// Convert back to the immutable form.
        /// </summary>
        ILaunchProfile ToLaunchProfile();
    }
}
