// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class LaunchSettings : ILaunchSettings
    {
        private readonly string _activeProfileName;

        /// <summary>
        /// Represents the current set of launch settings. Creation from an existing set of profiles. 
        /// </summary>
        public LaunchSettings(IEnumerable<ILaunchProfile> profiles, IDictionary<string, object> globalSettings, string activeProfile = null)
        {
            Profiles = ImmutableList<ILaunchProfile>.Empty;
            foreach (var profile in profiles)
            {
                Profiles = Profiles.Add(new LaunchProfile(profile));
            }

            GlobalSettings = globalSettings == null ? ImmutableStringDictionary<object>.EmptyOrdinal : globalSettings.ToImmutableDictionary();
            _activeProfileName = activeProfile;
        }

        public LaunchSettings(LaunchSettingsData settingsData, string activeProfile = null)
        {
            Profiles = ImmutableList<ILaunchProfile>.Empty;
            foreach (var profile in settingsData.Profiles)
            {
                Profiles = Profiles.Add(new LaunchProfile(profile));
            }

            GlobalSettings = settingsData.OtherSettings == null ? ImmutableStringDictionary<object>.EmptyOrdinal : settingsData.OtherSettings.ToImmutableDictionary();
            _activeProfileName = activeProfile;
        }

        public LaunchSettings(IWritableLaunchSettings settings)
        {
            Profiles = ImmutableList<ILaunchProfile>.Empty;
            foreach (var profile in settings.Profiles)
            {
                Profiles = Profiles.Add(new LaunchProfile(profile));
            }

            // For global settings we want to make new copies of each entry so that the snapshot remains immutable. If the object implements 
            // ICloneable that is used, otherwise, it is serialized back to json, and a new object rehydrated from that
            GlobalSettings = ImmutableStringDictionary<object>.EmptyOrdinal;
            foreach (var kvp in settings.GlobalSettings)
            {
                if (kvp.Value is ICloneable clonableObject)
                {
                    GlobalSettings = GlobalSettings.Add(kvp.Key, clonableObject.Clone());
                }
                else
                {
                    string jsonString = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                    object clonedObject = JsonConvert.DeserializeObject(jsonString, kvp.Value.GetType());
                    GlobalSettings = GlobalSettings.Add(kvp.Key, clonedObject);
                }
            }

            _activeProfileName = settings.ActiveProfile?.Name;
        }

        public LaunchSettings()
        {
            Profiles = ImmutableList<ILaunchProfile>.Empty;
            GlobalSettings = ImmutableStringDictionary<object>.EmptyOrdinal;
        }

        public ImmutableList<ILaunchProfile> Profiles { get; }

        public ImmutableDictionary<string, object> GlobalSettings { get; }

        public object GetGlobalSetting(string settingName)
        {
            GlobalSettings.TryGetValue(settingName, out object o);
            return o;
        }

        private ILaunchProfile _activeProfile;
        public ILaunchProfile ActiveProfile
        {
            get
            {
                if (_activeProfile == null)
                {
                    // If no active profile specifed, or the active one is no longer valid, assume the first one
                    if (!string.IsNullOrWhiteSpace(_activeProfileName))
                    {
                        _activeProfile = Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, _activeProfileName));
                    }

                    if (_activeProfile == null)
                    {
                        _activeProfile = Profiles.Count > 0 ? Profiles[0] : null;
                    }
                }

                return _activeProfile;
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
