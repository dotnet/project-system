// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Represents one launch profile read from the launchSettings file.
    /// </summary>
    internal class WritableLaunchProfile : IWritableLaunchProfile, IWritablePersistOption
    {
        public WritableLaunchProfile()
        {
            EnvironmentVariables = new(StringComparers.EnvironmentVariableNames);
            OtherSettings = new(StringComparers.LaunchSettingsPropertyNames);
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

            EnvironmentVariables = profile.GetEnvironmentVariablesDictionary() ?? new(StringComparers.EnvironmentVariableNames);
            OtherSettings = profile.GetOtherSettingsDictionary() ?? new(StringComparers.LaunchSettingsPropertyNames);
        }

        public string? Name { get; set; }
        public string? CommandName { get; set; }
        public string? ExecutablePath { get; set; }
        public string? CommandLineArgs { get; set; }
        public string? WorkingDirectory { get; set; }
        public bool LaunchBrowser { get; set; }
        public string? LaunchUrl { get; set; }
        public bool DoNotPersist { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; }
        public Dictionary<string, object> OtherSettings { get; }

        public ILaunchProfile ToLaunchProfile() => new LaunchProfile(this);
    }
}
