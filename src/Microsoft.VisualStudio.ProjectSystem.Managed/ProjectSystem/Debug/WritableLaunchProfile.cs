// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Collections;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Represents one launch profile read from the launchSettings file.
    /// </summary>
    internal class WritableLaunchProfile : IWritableLaunchProfile, IWritablePersistOption
    {
        public WritableLaunchProfile()
        {
        }

        public WritableLaunchProfile(ILaunchProfile profile)
        {
            Name = profile.Name;
            ExecutablePath = profile.ExecutablePath;
            CommandName = profile.CommandName;
            CommandLineArgs = profile.CommandLineArgs;
            WorkingDirectory = profile.WorkingDirectory;
            LaunchBrowser = profile.LaunchBrowser;
            LaunchUrl = profile.LaunchUrl;
            DoNotPersist = profile.IsInMemoryObject();

            if (profile.EnvironmentVariables != null)
            {
                EnvironmentVariables = new Dictionary<string, string>(profile.EnvironmentVariables, StringComparer.Ordinal);
            }

            if (profile.OtherSettings != null)
            {
                OtherSettings = new Dictionary<string, object>(profile.OtherSettings, StringComparers.LaunchProfileProperties);
            }
        }

        public string? Name { get; set; }
        public string? CommandName { get; set; }
        public string? ExecutablePath { get; set; }
        public string? CommandLineArgs { get; set; }
        public string? WorkingDirectory { get; set; }
        public bool LaunchBrowser { get; set; }
        public string? LaunchUrl { get; set; }
        public bool DoNotPersist { get; set; }

        public Dictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public Dictionary<string, object> OtherSettings { get; } = new Dictionary<string, object>(StringComparers.LaunchProfileProperties);

        /// <summary>
        /// Converts back to the immutable form
        /// </summary>
        public ILaunchProfile ToLaunchProfile()
        {
            return new LaunchProfile(this);
        }

        /// <summary>
        /// Compares two IWritableLaunchProfile to see if they contain the same values.
        /// </summary>
        public static bool ProfilesAreEqual(IWritableLaunchProfile debugProfile1, IWritableLaunchProfile debugProfile2)
        {
            // Same instance are equal
            if (ReferenceEquals(debugProfile1, debugProfile2))
            {
                return true;
            }

            if (!string.Equals(debugProfile1.Name, debugProfile2.Name, StringComparisons.LaunchProfileProperties) ||
               !string.Equals(debugProfile1.CommandName, debugProfile2.CommandName, StringComparisons.LaunchProfileCommandNames) ||
               !string.Equals(debugProfile1.ExecutablePath, debugProfile2.ExecutablePath, StringComparisons.LaunchProfileProperties) ||
               !string.Equals(debugProfile1.CommandLineArgs, debugProfile2.CommandLineArgs, StringComparisons.LaunchProfileProperties) ||
               !string.Equals(debugProfile1.WorkingDirectory, debugProfile2.WorkingDirectory, StringComparisons.LaunchProfileProperties) ||
               !string.Equals(debugProfile1.LaunchUrl, debugProfile2.LaunchUrl, StringComparisons.LaunchProfileProperties) ||
               debugProfile1.LaunchBrowser != debugProfile2.LaunchBrowser ||
               !DictionaryEqualityComparer<string, object>.Instance.Equals(debugProfile1.OtherSettings.ToImmutableDictionary(), debugProfile2.OtherSettings.ToImmutableDictionary()) ||
               !DictionaryEqualityComparer<string, string>.Instance.Equals(debugProfile1.EnvironmentVariables.ToImmutableDictionary(), debugProfile2.EnvironmentVariables.ToImmutableDictionary())
               )
            {
                return false;
            }

            // Compare in-memory states
            return debugProfile1.IsInMemoryObject() == debugProfile2.IsInMemoryObject();
        }
    }
}
