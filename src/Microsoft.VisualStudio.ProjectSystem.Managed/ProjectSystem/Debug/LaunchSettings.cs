// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Immutable snapshot of data from the <c>launchSettings.json</c> file.
    /// </summary>
    internal class LaunchSettings : ILaunchSettings, IVersionedLaunchSettings
    {
        private readonly string? _activeProfileName;

        /// <summary>
        /// Represents the current set of launch settings. Creation from an existing set of profiles.
        /// </summary>
        public LaunchSettings(IEnumerable<ILaunchProfile> profiles, ImmutableDictionary<string, object>? globalSettings, string? activeProfile = null, long version = 0)
        {
            Profiles = ImmutableList<ILaunchProfile>.Empty;
            foreach (ILaunchProfile profile in profiles)
            {
                Profiles = Profiles.Add(LaunchProfile.Clone(profile));
            }

            GlobalSettings = globalSettings ?? ImmutableStringDictionary<object>.EmptyOrdinal;
            _activeProfileName = activeProfile;
            Version = version;
        }

        public LaunchSettings(LaunchSettingsData settingsData, string? activeProfile, long version)
        {
            Requires.NotNull(settingsData.Profiles!, nameof(settingsData.Profiles));

            Profiles = ImmutableList<ILaunchProfile>.Empty;
            foreach (LaunchProfileData profile in settingsData.Profiles)
            {
                Profiles = Profiles.Add(new LaunchProfile(profile));
            }

            GlobalSettings = settingsData.OtherSettings == null ? ImmutableStringDictionary<object>.EmptyOrdinal : settingsData.OtherSettings.ToImmutableDictionary();
            _activeProfileName = activeProfile;
            Version = version;
        }

        public LaunchSettings(IWritableLaunchSettings settings, long version = 0)
        {
            Profiles = ImmutableList<ILaunchProfile>.Empty;
            foreach (IWritableLaunchProfile profile in settings.Profiles)
            {
                Profiles = Profiles.Add(new LaunchProfile(profile));
            }

            GlobalSettings = ImmutableStringDictionary<object>.EmptyOrdinal.AddRange(CloneGlobalSettingsValues(settings.GlobalSettings));
            _activeProfileName = settings.ActiveProfile?.Name;
            Version = version;
        }

        public LaunchSettings()
        {
            Profiles = ImmutableList<ILaunchProfile>.Empty;
            GlobalSettings = ImmutableStringDictionary<object>.EmptyOrdinal;
            Version = 0;
        }

        public ImmutableList<ILaunchProfile> Profiles { get; }

        public ImmutableDictionary<string, object> GlobalSettings { get; }

        public object? GetGlobalSetting(string settingName)
        {
            GlobalSettings.TryGetValue(settingName, out object? o);
            return o;
        }

        private ILaunchProfile? _activeProfile;
        public ILaunchProfile? ActiveProfile
        {
            get
            {
                if (_activeProfile == null)
                {
                    ILaunchProfile? computedProfile = null;

                    // If no active profile specified, or the active one is no longer valid, assume the first one
                    if (!Strings.IsNullOrWhiteSpace(_activeProfileName))
                    {
                        computedProfile = Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, _activeProfileName));
                    }

                    computedProfile ??= !Profiles.IsEmpty ? Profiles[0] : null;

                    Interlocked.CompareExchange(ref _activeProfile, value: computedProfile, comparand: null);
                }

                return _activeProfile;
            }
        }

        public long Version { get; }

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
