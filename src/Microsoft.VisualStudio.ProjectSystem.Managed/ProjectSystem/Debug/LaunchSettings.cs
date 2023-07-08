// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Immutable snapshot of data from the <c>launchSettings.json</c> file.
    /// </summary>
    internal class LaunchSettings : ILaunchSettings, IVersionedLaunchSettings
    {
        public static LaunchSettings Empty { get; } = new();

        public LaunchSettings(
            IEnumerable<ILaunchProfile>? profiles = null,
            ImmutableDictionary<string, object>? globalSettings = null,
            string? activeProfileName = null,
            ILaunchProfile? launchProfile = null,
            long version = 0)
        {
            Profiles = profiles is null
                ? ImmutableList<ILaunchProfile>.Empty
                : ImmutableList.CreateRange<ILaunchProfile>(profiles.Select(LaunchProfile.Clone));
            GlobalSettings = globalSettings ?? ImmutableStringDictionary<object>.EmptyOrdinal;
            ActiveProfile = launchProfile ?? FindActiveProfile();
            Version = version;

            ILaunchProfile? FindActiveProfile()
            {
                ILaunchProfile? profile = null;

                if (!Profiles.IsEmpty)
                {
                    if (!Strings.IsNullOrWhiteSpace(activeProfileName))
                    {
                        // Find the first profile having the required name
                        profile = Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, activeProfileName));
                    }

                    // If no active profile specified, or the active one is no longer valid, assume the first one
                    profile ??= Profiles[0];
                }

                return profile;
            }
        }

        public ImmutableList<ILaunchProfile> Profiles { get; }

        public ImmutableDictionary<string, object> GlobalSettings { get; }

        public ILaunchProfile? ActiveProfile { get; }

        public long Version { get; }

        public object? GetGlobalSetting(string settingName)
        {
            GlobalSettings.TryGetValue(settingName, out object? o);
            return o;
        }

        /// <summary>
        /// Produces a sequence of values cloned from <paramref name="keyValues"/>. Used to keep snapshots of
        /// launch setting data immutable. <see langword="null"/> values are excluded from the output sequence.
        /// </summary>
        /// <remarks>
        /// The following approach is taken:
        /// <list type="number">
        ///   <item>Common known immutable types (<see cref="string"/>, <see cref="int"/>, <see cref="bool"/>) are not cloned.</item>
        ///   <item>If the type supports <see cref="ICloneable"/>, that is used.</item>
        ///   <item>Otherwise the object is round tripped to JSON and back to create a clone.</item>
        /// </list>
        /// </remarks>
        internal static IEnumerable<KeyValuePair<string, object>> CloneGlobalSettingsValues(IEnumerable<KeyValuePair<string, object>> keyValues)
        {
            JsonSerializerSettings? jsonSerializerSettings = null;

            foreach ((string key, object value) in keyValues)
            {
                if (value is int or string or bool)
                {
                    // These common types do not need cloning.
                    yield return new(key, value);
                }
                else if (value is ICloneable cloneableObject)
                {
                    // Type supports cloning.
                    yield return new(key, cloneableObject.Clone());
                }
                else if (value is not null)
                {
                    // Custom type. The best way we have to clone it is to round trip it to JSON and back.
                    jsonSerializerSettings ??= new() { NullValueHandling = NullValueHandling.Ignore };

                    string jsonString = JsonConvert.SerializeObject(value, Formatting.Indented, jsonSerializerSettings);

                    object? clonedObject = JsonConvert.DeserializeObject(jsonString, value.GetType());

                    if (clonedObject is not null)
                    {
                        yield return new(key, clonedObject);
                    }
                }

                // Null values are skipped.
            }
        }
    }

    internal static class LaunchSettingsExtension
    {
        public static IWritableLaunchSettings ToWritableLaunchSettings(this ILaunchSettings curSettings)
        {
            return new WritableLaunchSettings(curSettings);
        }
    }
}
