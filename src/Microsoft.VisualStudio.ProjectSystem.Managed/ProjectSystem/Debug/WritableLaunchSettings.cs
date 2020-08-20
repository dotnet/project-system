// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class WritableLaunchSettings : IWritableLaunchSettings
    {
        public WritableLaunchSettings(ILaunchSettings settings)
        {
            if (settings.Profiles != null)
            {
                foreach (ILaunchProfile profile in settings.Profiles)
                {
                    Profiles.Add(new WritableLaunchProfile(profile));
                }
            }

            // For global settings we want to make new copies of each entry so that the snapshot remains immutable. If the object implements 
            // ICloneable that is used, otherwise, it is serialized back to json, and a new object rehydrated from that
            if (settings.GlobalSettings != null)
            {
                var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

                foreach ((string key, object value) in settings.GlobalSettings)
                {
                    if (value is ICloneable cloneableObject)
                    {
                        GlobalSettings.Add(key, cloneableObject.Clone());
                    }
                    else
                    {
                        string jsonString = JsonConvert.SerializeObject(value, Formatting.Indented, jsonSerializerSettings);
                        object clonedObject = JsonConvert.DeserializeObject(jsonString, value.GetType());
                        GlobalSettings.Add(key, clonedObject);
                    }
                }
            }

            if (settings.ActiveProfile != null)
            {
                ActiveProfile = Profiles.Find(profile => LaunchProfile.IsSameProfileName(profile.Name, settings.ActiveProfile.Name));
            }
        }

        public IWritableLaunchProfile? ActiveProfile { get; set; }

        public List<IWritableLaunchProfile> Profiles { get; } = new List<IWritableLaunchProfile>();

        public Dictionary<string, object> GlobalSettings { get; } = new Dictionary<string, object>(StringComparers.LaunchProfileProperties);

        public ILaunchSettings ToLaunchSettings()
        {
            return new LaunchSettings(this);
        }
    }
}
