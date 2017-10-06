// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            if(profile.EnvironmentVariables  != null)
            {
                EnvironmentVariables = new Dictionary<string, string>(profile.EnvironmentVariables, StringComparer.Ordinal);
            }

            if(profile.OtherSettings  != null)
            {
                OtherSettings = new Dictionary<string, object>(profile.OtherSettings, StringComparer.Ordinal);
            }
        }

        public string Name { get; set; }
        public string CommandName { get; set; }
        public string ExecutablePath { get; set; }
        public string CommandLineArgs { get; set; }
        public string WorkingDirectory { get; set; }
        public bool LaunchBrowser { get; set; }
        public string LaunchUrl { get; set; }
        public bool DoNotPersist { get; set; }

        public Dictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public Dictionary<string, object> OtherSettings { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

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
            if (debugProfile1 == debugProfile2)
            {
                return true;
            }

            if (!string.Equals(debugProfile1.Name, debugProfile2.Name, StringComparison.Ordinal) ||
               !string.Equals(debugProfile1.CommandName, debugProfile2.CommandName, StringComparison.Ordinal) ||
               !string.Equals(debugProfile1.ExecutablePath, debugProfile2.ExecutablePath, StringComparison.Ordinal) ||
               !string.Equals(debugProfile1.CommandLineArgs, debugProfile2.CommandLineArgs, StringComparison.Ordinal) ||
               !string.Equals(debugProfile1.WorkingDirectory, debugProfile2.WorkingDirectory, StringComparison.Ordinal) ||
               !string.Equals(debugProfile1.LaunchUrl, debugProfile2.LaunchUrl, StringComparison.Ordinal) ||
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
