// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Represents one launch profile read from the launchSettings file.
    /// </summary>
    internal class LaunchProfile : ILaunchProfile, IPersistOption
    {
        /// <summary>
        /// Creates a copy of <paramref name="profile"/> in which tokens are replaced via <paramref name="replaceAsync"/>.
        /// </summary>
        /// <remarks>
        /// Intended to replace tokens such as environment variables and MSBuild properties.
        /// </remarks>
        /// <param name="profile">The source profile to copy from.</param>
        /// <param name="replaceAsync">A function that performs token substitution.</param>
        /// <returns>A profile with tokens substituted.</returns>
        internal static async Task<LaunchProfile> ReplaceTokensAsync(ILaunchProfile profile, Func<string, Task<string>> replaceAsync)
        {
            ImmutableDictionary<string, string>? environmentVariables = profile.EnvironmentVariables;
            ImmutableDictionary<string, object>? otherSettings = profile.OtherSettings;

            if (environmentVariables != null)
            {
                foreach ((string key, string value) in environmentVariables)
                {
                    environmentVariables = environmentVariables.SetItem(key, await replaceAsync(value));
                }
            }

            if (otherSettings != null)
            {
                foreach ((string key, object value) in otherSettings)
                {
                    if (value is string s)
                    {
                        otherSettings = otherSettings.SetItem(key, await replaceAsync(s));
                    }
                }
            }

            ImmutableArray<string> environmentVariablesKeyOrder;
            ImmutableArray<string> otherSettingsKeyOrder;

            if (profile is LaunchProfile launchProfile)
            {
                // Preserve the order of keys
                environmentVariablesKeyOrder = launchProfile.EnvironmentVariablesKeyOrder;
                otherSettingsKeyOrder = launchProfile.OtherSettingsKeyOrder;
            }
            else
            {
                // We don't know the order here, so just use dictionary order (which will be randomised by hash codes)
                environmentVariablesKeyOrder = GetKeys(environmentVariables);
                otherSettingsKeyOrder = GetKeys(otherSettings);
            }

            return new(
                name: profile.Name,
                commandName: profile.CommandName,
                executablePath: await ReplaceOrNullAsync(profile.ExecutablePath),
                commandLineArgs: await ReplaceOrNullAsync(profile.CommandLineArgs),
                workingDirectory: await ReplaceOrNullAsync(profile.WorkingDirectory),
                launchBrowser: profile.LaunchBrowser,
                launchUrl: await ReplaceOrNullAsync(profile.LaunchUrl),
                doNotPersist: profile.IsInMemoryObject(),
                environmentVariables: environmentVariables,
                otherSettings: otherSettings,
                environmentVariablesKeyOrder: environmentVariablesKeyOrder,
                otherSettingsKeyOrder: otherSettingsKeyOrder);

            Task<string?> ReplaceOrNullAsync(string? s)
            {
                if (Strings.IsNullOrWhiteSpace(s))
                {
                    return TaskResult.Null<string>();
                }

                return replaceAsync(s)!;
            }
        }

        public LaunchProfile(
            string? name,
            string? commandName,
            string? executablePath = null,
            string? commandLineArgs = null,
            string? workingDirectory = null,
            bool launchBrowser = false,
            string? launchUrl = null,
            bool doNotPersist = false,
            Dictionary<string, string>? environmentVariables = null,
            Dictionary<string, object>? otherSettings = null)
        {
            Name = name;
            CommandName = commandName;
            ExecutablePath = executablePath;
            CommandLineArgs = commandLineArgs;
            WorkingDirectory = workingDirectory;
            LaunchBrowser = launchBrowser;
            LaunchUrl = launchUrl;
            DoNotPersist = doNotPersist;

            AssignFromDictionaries(environmentVariables, otherSettings);
        }

        private LaunchProfile(
            string? name,
            string? commandName,
            string? executablePath,
            string? commandLineArgs,
            string? workingDirectory,
            bool launchBrowser,
            string? launchUrl,
            bool doNotPersist,
            ImmutableDictionary<string, string>? environmentVariables,
            ImmutableDictionary<string, object>? otherSettings,
            ImmutableArray<string> environmentVariablesKeyOrder,
            ImmutableArray<string> otherSettingsKeyOrder)
        {
            Name = name;
            CommandName = commandName;
            ExecutablePath = executablePath;
            CommandLineArgs = commandLineArgs;
            WorkingDirectory = workingDirectory;
            LaunchBrowser = launchBrowser;
            LaunchUrl = launchUrl;
            DoNotPersist = doNotPersist;
            EnvironmentVariables = environmentVariables;
            OtherSettings = otherSettings;
            EnvironmentVariablesKeyOrder = environmentVariablesKeyOrder;
            OtherSettingsKeyOrder = otherSettingsKeyOrder;
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
            DoNotPersist = data.InMemoryProfile;

            AssignFromDictionaries(data.EnvironmentVariables, data.OtherSettings);
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
            DoNotPersist = existingProfile.IsInMemoryObject();

            EnvironmentVariables = existingProfile.EnvironmentVariables;
            OtherSettings = existingProfile.OtherSettings;

            if (existingProfile is LaunchProfile launchProfile)
            {
                // Preserve the order of items.
                EnvironmentVariablesKeyOrder = launchProfile.EnvironmentVariablesKeyOrder;
                OtherSettingsKeyOrder = launchProfile.OtherSettingsKeyOrder;
            }
            else
            {
                // We are unable to preserve order on other implementations of ILaunchProfile.
                // In future we could version the interface to support this.
                EnvironmentVariablesKeyOrder = GetKeys(existingProfile.EnvironmentVariables);
                OtherSettingsKeyOrder = GetKeys(existingProfile.OtherSettings);
            }
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

            AssignFromDictionaries(writableProfile.EnvironmentVariables, writableProfile.OtherSettings);
        }

        private void AssignFromDictionaries(Dictionary<string, string>? environmentVariables, Dictionary<string, object>? otherSettings)
        {
            // If there are no env variables or settings we want to set them to null
            EnvironmentVariables = environmentVariables is { Count: not 0 } envVars ? envVars.ToImmutableDictionary() : null;
            OtherSettings = otherSettings is { Count: not 0 } others ? others.ToImmutableDictionary() : null;

            // Dictionary<,> maintains the order of its keys, while ImmutableDictionary<,> does not.
            // We convert to immutable here, and also retain the keys in their original order.
            // This allows us to round-trip these collections without reordering their values.
            // Ideally we would have declared these properties as IImmutableDictionary<,> and used
            // an order preserving implementation, but the interface is public and has already shipped
            // so cannot be changed.
            EnvironmentVariablesKeyOrder = GetKeys(environmentVariables);
            OtherSettingsKeyOrder = GetKeys(otherSettings);
        }

        public string? Name { get; }
        public string? CommandName { get; }
        public string? ExecutablePath { get; }
        public string? CommandLineArgs { get; }
        public string? WorkingDirectory { get; }
        public bool LaunchBrowser { get; }
        public string? LaunchUrl { get; }
        public bool DoNotPersist { get; }

        public ImmutableDictionary<string, string>? EnvironmentVariables { get; private set; }
        public ImmutableDictionary<string, object>? OtherSettings { get; private set; }

        public ImmutableArray<string> EnvironmentVariablesKeyOrder { get; private set; }
        public ImmutableArray<string> OtherSettingsKeyOrder { get; private set; }

        /// <summary>
        /// Compares two profile names. Using this function ensures case comparison consistency
        /// </summary>
        public static bool IsSameProfileName(string? name1, string? name2)
        {
            return string.Equals(name1, name2, StringComparisons.LaunchProfileNames);
        }

        private static ImmutableArray<string> GetKeys<T>(IReadOnlyDictionary<string, T>? dic)
        {
            return dic?.Keys.OrderBy(key => key).ToImmutableArray() ?? ImmutableArray<string>.Empty;
        }
    }
}
