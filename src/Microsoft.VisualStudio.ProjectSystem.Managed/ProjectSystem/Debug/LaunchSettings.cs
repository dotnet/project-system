// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
            foreach(var profile in profiles)
            {
                Profiles = Profiles.Add(new LaunchProfile(profile));
            }

            GlobalSettings = globalSettings == null? ImmutableDictionary<string, object>.Empty : globalSettings.ToImmutableDictionary();
            _activeProfileName = activeProfile;
        }

        public LaunchSettings(LaunchSettingsData settingsData, string activeProfile = null)
        {
            Profiles = ImmutableList<ILaunchProfile>.Empty;
            foreach(var profile in settingsData.Profiles)
            {
                Profiles = Profiles.Add(new LaunchProfile(profile));
            }
            
            GlobalSettings = settingsData.OtherSettings == null? ImmutableDictionary<string, object>.Empty : settingsData.OtherSettings.ToImmutableDictionary();
            _activeProfileName = activeProfile;
        }

        public LaunchSettings()
        {
            Profiles = ImmutableList<ILaunchProfile>.Empty;
            GlobalSettings = ImmutableDictionary<string, object>.Empty;
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
                if(_activeProfile == null)
                {
                    // If no active profile specifed, or the active one is no longer valid, assume the first one
                    if(!string.IsNullOrWhiteSpace(_activeProfileName))
                    {
                        _activeProfile = Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, _activeProfileName));
                    }

                    if(_activeProfile == null)
                    {
                        _activeProfile =  Profiles.Count > 0 ? Profiles[0] : null;
                    }
                }

                return _activeProfile;
            }
         }
    }
    internal static class LaunchSettingsExtension
    {
        public static bool ProfilesAreDifferent(this ILaunchSettings launchSettings, IList<ILaunchProfile> profilesToCompare)
        {
            bool detectedChanges = launchSettings.Profiles == null || launchSettings.Profiles.Count != profilesToCompare.Count;
            if (!detectedChanges)
            {
                // Now compare each item
                foreach (var profile in profilesToCompare)
                {
                    var existingProfile = launchSettings.Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, profile.Name));
                    if (existingProfile == null || !LaunchProfile.ProfilesAreEqual(profile, existingProfile, true))
                    {
                        detectedChanges = true;
                        break;
                    }
                }
            }
            return detectedChanges;
        }
    }
}
