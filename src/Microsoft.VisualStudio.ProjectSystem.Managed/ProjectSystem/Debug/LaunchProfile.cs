// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Represents one launch profile read from the launchSettings file.
    /// </summary>
    internal class LaunchProfile : ILaunchProfile2, IPersistOption
    {
        public static LaunchProfile Clone(ILaunchProfile profile)
        {
            // LaunchProfile is immutable and doesn't need to be cloned.
            if (profile is LaunchProfile lp)
            {
                return lp;
            }

            // Unknown implementation. Make a defensive copy to a new immutable instance.
            return new LaunchProfile(
                name: profile.Name,
                executablePath: profile.ExecutablePath,
                commandName: profile.CommandName,
                commandLineArgs: profile.CommandLineArgs,
                workingDirectory: profile.WorkingDirectory,
                launchBrowser: profile.LaunchBrowser,
                launchUrl: profile.LaunchUrl,
                environmentVariables: profile.FlattenEnvironmentVariables(),
                otherSettings: profile.FlattenOtherSettings(),
                doNotPersist: profile.IsInMemoryObject());
        }

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
            return new(
                name: profile.Name,
                commandName: profile.CommandName,
                executablePath: await ReplaceOrNullAsync(profile.ExecutablePath),
                commandLineArgs: await ReplaceOrNullAsync(profile.CommandLineArgs),
                workingDirectory: await ReplaceOrNullAsync(profile.WorkingDirectory),
                launchBrowser: profile.LaunchBrowser,
                launchUrl: await ReplaceOrNullAsync(profile.LaunchUrl),
                doNotPersist: profile.IsInMemoryObject(),
                environmentVariables: await GetEnvironmentVariablesAsync(),
                otherSettings: await GetOtherSettingsAsync());

            Task<string?> ReplaceOrNullAsync(string? s)
            {
                if (Strings.IsNullOrWhiteSpace(s))
                {
                    return TaskResult.Null<string>();
                }

                return replaceAsync(s)!;
            }

            Task<ImmutableArray<(string Key, string Value)>> GetEnvironmentVariablesAsync()
            {
                return profile switch
                {
                    ILaunchProfile2 profile2 => ReplaceValuesAsync(profile2.EnvironmentVariables, replaceAsync),
                    _ => ReplaceValuesAsync(profile.FlattenEnvironmentVariables(), replaceAsync)
                };
            }

            Task<ImmutableArray<(string Key, object Value)>> GetOtherSettingsAsync()
            {
                return profile switch
                {
                    ILaunchProfile2 profile2 => ReplaceValuesAsync(profile2.OtherSettings, ReplaceIfString),
                    _ => ReplaceValuesAsync(profile.FlattenOtherSettings(), ReplaceIfString)
                };

                async Task<object> ReplaceIfString(object o)
                {
                    return o switch
                    {
                        string s => await replaceAsync(s),
                        _ => o
                    };
                }
            }

            static async Task<ImmutableArray<(string Key, T Value)>> ReplaceValuesAsync<T>(ImmutableArray<(string Key, T Value)> source, Func<T, Task<T>> replaceAsync)
                where T : class
            {
                // We will only allocate a new array if a substituion is made
                ImmutableArray<(string, T)>.Builder? builder = null;

                for (int index = 0; index < source.Length; index++)
                {
                    (string key, T value) = source[index];

                    T replaced = await replaceAsync(value);

                    if (!ReferenceEquals(value, replaced))
                    {
                        // The value had at least one token substitution.
                        if (builder is null)
                        {
                            // Init the builder.
                            builder = ImmutableArray.CreateBuilder<(string, T)>(source.Length);

                            if (index != 0)
                            {
                                // Copy any unsubstituted values up until this point.
                                builder.AddRange(source, index);
                            }
                        }
                    }

                    builder?.Add((key, (T)replaced));
                }

                // Return the source unchanged if there were no substitutions made.
                return builder?.MoveToImmutable() ?? source;
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
            ImmutableArray<(string Key, string Value)> environmentVariables = default,
            ImmutableArray<(string Key, object Value)> otherSettings = default)
        {
            Name = name;
            CommandName = commandName;
            ExecutablePath = executablePath;
            CommandLineArgs = commandLineArgs;
            WorkingDirectory = workingDirectory;
            LaunchBrowser = launchBrowser;
            LaunchUrl = launchUrl;
            DoNotPersist = doNotPersist;

            EnvironmentVariables = environmentVariables.IsDefault
                ? ImmutableArray<(string Key, string Value)>.Empty
                : environmentVariables;
            OtherSettings = otherSettings.IsDefault
                ? ImmutableArray<(string Key, object Value)>.Empty
                : otherSettings;
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
            EnvironmentVariables = Flatten(data.EnvironmentVariables);
            OtherSettings = Flatten(data.OtherSettings);
            DoNotPersist = data.InMemoryProfile;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LaunchProfile"/>,
        /// copying data from the mutable <paramref name="writableProfile"/>.
        /// </summary>
        public LaunchProfile(IWritableLaunchProfile writableProfile)
        {
            Name = writableProfile.Name;
            ExecutablePath = writableProfile.ExecutablePath;
            CommandName = writableProfile.CommandName;
            CommandLineArgs = writableProfile.CommandLineArgs;
            WorkingDirectory = writableProfile.WorkingDirectory;
            LaunchBrowser = writableProfile.LaunchBrowser;
            LaunchUrl = writableProfile.LaunchUrl;
            EnvironmentVariables = Flatten(writableProfile.EnvironmentVariables);
            OtherSettings = Flatten(writableProfile.OtherSettings);
            DoNotPersist = writableProfile.IsInMemoryObject();
        }

        private static ImmutableArray<(string, T)> Flatten<T>(Dictionary<string, T>? dictionary)
        {
            if (dictionary is null)
            {
                return ImmutableArray<(string, T)>.Empty;
            }

            ImmutableArray<(string, T)>.Builder builder = ImmutableArray.CreateBuilder<(string, T)>(dictionary.Count);

            foreach ((string key, T value) in dictionary)
            {
                builder.Add(new(key, value));
            }

            return builder.MoveToImmutable();
        }

        public string? Name { get; }
        public string? CommandName { get; }
        public string? ExecutablePath { get; }
        public string? CommandLineArgs { get; }
        public string? WorkingDirectory { get; }
        public bool LaunchBrowser { get; }
        public string? LaunchUrl { get; }
        public bool DoNotPersist { get; }

        public ImmutableArray<(string Key, string Value)> EnvironmentVariables { get; }
        public ImmutableArray<(string Key, object Value)> OtherSettings { get; }

        ImmutableDictionary<string, string>? ILaunchProfile.EnvironmentVariables => EnvironmentVariables.ToImmutableDictionary(pairs => pairs.Key, pairs => pairs.Value, StringComparers.EnvironmentVariableNames);
        ImmutableDictionary<string, object>? ILaunchProfile.OtherSettings => OtherSettings.ToImmutableDictionary(pairs => pairs.Key, pairs => pairs.Value, StringComparers.LaunchProfileProperties);

        /// <summary>
        /// Compares two profile names. Using this function ensures case comparison consistency
        /// </summary>
        public static bool IsSameProfileName(string? name1, string? name2)
        {
            return string.Equals(name1, name2, StringComparisons.LaunchProfileNames);
        }
    }
}
