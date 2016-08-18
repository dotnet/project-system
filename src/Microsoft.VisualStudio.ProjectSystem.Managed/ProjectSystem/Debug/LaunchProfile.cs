// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    /// <summary>
    /// Represents one launch profile read from the launchSettings file.
    /// </summary>
    internal class LaunchProfile : ILaunchProfile
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
            LaunchBrowser = data.LaunchBrowser?? false;
            LaunchUrl = data.LaunchUrl;
            EnvironmentVariables = data.EnvironmentVariables == null ? null : ImmutableDictionary<string, string>.Empty.AddRange(data.EnvironmentVariables);
            OtherSettings = data.OtherSettings == null ? null : ImmutableDictionary<string, object>.Empty.AddRange(data.OtherSettings);
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
        }

        /// <summary>
        /// IDebug profile members
        /// </summary>
        public string Name { get; set; }
        public string CommandName { get; set; }
        public string ExecutablePath { get; set; }
        public string CommandLineArgs { get; set; }
        public string WorkingDirectory { get; set; }
        public bool LaunchBrowser { get; set; }
        public string LaunchUrl { get; set; }

        private IDictionary<string, string> _environmentVariables;
        public ImmutableDictionary<string, string> EnvironmentVariables
        {
            get
            {
                return _environmentVariables == null ?
                    null: ImmutableDictionary<string, string>.Empty.AddRange(_environmentVariables);
            }
            set
            {
                _environmentVariables = value;
            }
        }

        private IDictionary<string, object> _otherSettings;
        public ImmutableDictionary<string, object> OtherSettings
        {
            get
            {
                return _otherSettings == null ?
                    null: ImmutableDictionary<string, object>.Empty.AddRange(_otherSettings);
            }
            set
            {
                _otherSettings = value;
            }
        }

        /// <summary>
        /// Compares two profile names. Using this function ensures case comparison consistency
        /// </summary>
        public static bool IsSameProfileName(string name1, string name2)
        {
            return string.Equals(name1, name2, StringComparison.Ordinal);
        }
    }
}
