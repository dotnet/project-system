// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Represents one launch profile read from the launchSettings file.
    /// </summary>
    internal class LaunchProfile : ILaunchProfile, IPersistOption
    {
        public LaunchProfile()
        {
        }

        public LaunchProfile(LaunchProfileData data)
        {
            Name = data.Name;
            ExecutablePath = data.ExecutablePath;
            CommandName = data.CommandName;
            CommandLineArgs = data.CommandLineArgs;
            WorkingDirectory = data.WorkingDirectory;
            LaunchBrowser = data.LaunchBrowser ?? false;
            LaunchUrl = data.LaunchUrl;
            EnvironmentVariables = data.EnvironmentVariables?.ToImmutableDictionary();
            OtherSettings = data.OtherSettings?.ToImmutableDictionary();
            DoNotPersist = data.InMemoryProfile;
        }

        /// <summary>
        /// Useful to create a mutable version from an existing immutable profile
        /// </summary>
        public LaunchProfile(ILaunchProfile existingProfile)
        {
            Name = existingProfile.Name;
            ExecutablePath = existingProfile.ExecutablePath;
            CommandName = existingProfile.CommandName;
            CommandLineArgs = existingProfile.CommandLineArgs;
            WorkingDirectory = existingProfile.WorkingDirectory;
            LaunchBrowser = existingProfile.LaunchBrowser;
            LaunchUrl = existingProfile.LaunchUrl;
            EnvironmentVariables = existingProfile.EnvironmentVariables;
            OtherSettings = existingProfile.OtherSettings;
            DoNotPersist = existingProfile.IsInMemoryObject();
        }

        public LaunchProfile(IWritableLaunchProfile writableProfile)
        {
            Name = writableProfile.Name;
            ExecutablePath = writableProfile.ExecutablePath;
            CommandName = writableProfile.CommandName;
            CommandLineArgs = writableProfile.CommandLineArgs;
            WorkingDirectory = writableProfile.WorkingDirectory;
            LaunchBrowser = writableProfile.LaunchBrowser;
            LaunchUrl = writableProfile.LaunchUrl;
            DoNotPersist = writableProfile.IsInMemoryObject();

            // If there are no env variables or settings we want to set them to null
            EnvironmentVariables = writableProfile.EnvironmentVariables.Count == 0 ? null : writableProfile.EnvironmentVariables.ToImmutableDictionary();
            OtherSettings = writableProfile.OtherSettings.Count == 0 ? null : writableProfile.OtherSettings.ToImmutableDictionary();
        }

        public string? Name { get; set; }
        public string? CommandName { get; set; }
        public string? ExecutablePath { get; set; }
        public string? CommandLineArgs { get; set; }
        public string? WorkingDirectory { get; set; }
        public bool LaunchBrowser { get; set; }
        public string? LaunchUrl { get; set; }
        public bool DoNotPersist { get; set; }

        public ImmutableDictionary<string, string>? EnvironmentVariables { get; set; }
        public ImmutableDictionary<string, object>? OtherSettings { get; set; }

        /// <summary>
        /// Compares two profile names. Using this function ensures case comparison consistency
        /// </summary>
        public static bool IsSameProfileName(string? name1, string? name2)
        {
            return string.Equals(name1, name2, StringComparisons.LaunchProfileNames);
        }
    }
}
