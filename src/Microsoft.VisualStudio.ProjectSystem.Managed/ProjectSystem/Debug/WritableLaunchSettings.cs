// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Collections;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class WritableLaunchSettings : IWritableLaunchSettings
    {
        public WritableLaunchSettings()
        {
        }

        public WritableLaunchSettings(ILaunchSettings settings)
        {
            if(settings.Profiles != null)
            {
                foreach(var profile in settings.Profiles)
                {
                    Profiles.Add(new WritableLaunchProfile(profile));
                }
            }
            
            // For global settings we want to make new copies of each entry so that the snapshot remains immutable. If the object implements 
            // ICloneable that is used, otherwise, it is serialized back to json, and a new object rehydrated from that
            if(settings.GlobalSettings != null)
            {    
                foreach(var kvp in settings.GlobalSettings)
                {
                    if(kvp.Value is ICloneable clonableObject)
                    {
                        GlobalSettings.Add(kvp.Key, clonableObject.Clone());
                    }
                    else
                    {
                        string jsonString = JsonConvert.SerializeObject(kvp.Value, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore});
                        object clonedObject = JsonConvert.DeserializeObject(jsonString, kvp.Value.GetType());
                        GlobalSettings.Add(kvp.Key, clonedObject);
                    }
                }
            }

            if(settings.ActiveProfile != null)
            {
                ActiveProfile = Profiles.FirstOrDefault((profile) => LaunchProfile.IsSameProfileName(profile.Name, settings.ActiveProfile.Name));
            }
        }

        public IWritableLaunchProfile ActiveProfile { get; set; }

        public List<IWritableLaunchProfile> Profiles { get; } = new List<IWritableLaunchProfile>();
        public Dictionary<String, object> GlobalSettings { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        public ILaunchSettings ToLaunchSettings()
        {
            return new LaunchSettings(this);
        }
    }

    internal static class WritableLaunchSettingsExtension
    {
        public static bool SetttingsDiffer(this IWritableLaunchSettings launchSettings, IWritableLaunchSettings settingsToCompare)
        {
            if(launchSettings.Profiles.Count != settingsToCompare.Profiles.Count)
            {
                return true;
            }

            // Now compare each item. We can compare in order. If the lists are different then the settings are different even
            // if they contain the same items
            for (int i=0; i< launchSettings.Profiles.Count; i++)
            {
                if (!WritableLaunchProfile.ProfilesAreEqual(launchSettings.Profiles[i], settingsToCompare.Profiles[i]))
                {
                    return true;
                }
            }
            
            // Check the global settings
            return DictionaryEqualityComparer<string, object>.Instance.Equals(launchSettings.GlobalSettings.ToImmutableDictionary(), settingsToCompare.GlobalSettings.ToImmutableDictionary());
        }

    }
}
