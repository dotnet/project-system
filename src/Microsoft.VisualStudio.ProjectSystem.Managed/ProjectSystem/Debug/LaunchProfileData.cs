// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class LaunchProfileData
    {
        // TODO do we even need this type?

        public string? Name { get; set; }

        public bool InMemoryProfile { get; set; }

        public string? CommandName { get; set; }
        public string? ExecutablePath { get; set; }
        public string? CommandLineArgs { get; set; }
        public string? WorkingDirectory { get; set; }
        public bool LaunchBrowser { get; set; }
        public string? LaunchUrl { get; set; }
        public Dictionary<string, string>? EnvironmentVariables { get; set; }
        public Dictionary<string, object>? OtherSettings { get; set; }

        /// <summary>
        /// Converts <paramref name="profile"/> to its serializable form.
        /// It does some fix up, like setting empty values to <see langword="null"/>.
        /// </summary>
        public static LaunchProfileData FromILaunchProfile(ILaunchProfile profile)
        {
            return new()
            {
                Name = profile.Name,
                ExecutablePath = profile.ExecutablePath,
                CommandName = profile.CommandName,
                CommandLineArgs = profile.CommandLineArgs,
                WorkingDirectory = profile.WorkingDirectory,
                LaunchBrowser = profile.LaunchBrowser,
                LaunchUrl = profile.LaunchUrl,
                EnvironmentVariables = profile.GetEnvironmentVariablesDictionary(),
                OtherSettings = profile.GetOtherSettingsDictionary(),
                InMemoryProfile = profile.IsInMemoryObject()
            };
        }
    }
}
