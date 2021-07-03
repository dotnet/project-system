// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class LaunchSettings : ILaunchSettings, IVersionedLaunchSettings
    {
        private readonly string? _activeProfileName;

        /// <summary>
        /// Represents the current set of launch settings. Creation from an existing set of profiles.
        /// </summary>
        public LaunchSettings(IEnumerable<ILaunchProfile> profiles, IDictionary<string, object>? globalSettings, string? activeProfile = null, long version = 0)
        {
            Profiles = ImmutableList<ILaunchProfile>.Empty;
            foreach (ILaunchProfile profile in profiles)
            {
                Profiles = Profiles.Add(new LaunchProfile(profile));
            }

            GlobalSettings = globalSettings == null ? ImmutableStringDictionary<object>.EmptyOrdinal : globalSettings.ToImmutableDictionary();
            _activeProfileName = activeProfile;
            Version = version;
        }

        public LaunchSettings(LaunchSettingsData settingsData, string? activeProfile = null, long version = 0)
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

            var jsonSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

            // For global settings we want to make new copies of each entry so that the snapshot remains immutable. If the object implements 
            // ICloneable that is used, otherwise, it is serialized back to json, and a new object rehydrated from that
            GlobalSettings = ImmutableStringDictionary<object>.EmptyOrdinal;
            foreach ((string key, object value) in settings.GlobalSettings)
            {
                if (value is ICloneable cloneableObject)
                {
                    GlobalSettings = GlobalSettings.Add(key, cloneableObject.Clone());
                }
                else
                {
                    string jsonString = JsonConvert.SerializeObject(value, Formatting.Indented, jsonSerializerSettings);
                    object clonedObject = JsonConvert.DeserializeObject(jsonString, value.GetType());
                    GlobalSettings = GlobalSettings.Add(key, clonedObject);
                }
            }

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
                    // If no active profile specified, or the active one is no longer valid, assume the first one
                    if (!Strings.IsNullOrWhiteSpace(_activeProfileName))
                    {
                        _activeProfile = Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, _activeProfileName));
                    }

                    _activeProfile ??= !Profiles.IsEmpty ? Profiles[0] : null;
                }

                return _activeProfile;
            }
        }

        public long Version { get; }
    }

    internal static class LaunchSettingsExtension
    {
        public static IWritableLaunchSettings ToWritableLaunchSettings(this ILaunchSettings curSettings)
        {
            return new WritableLaunchSettings(curSettings);
        }
    }
}
