// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    /// <summary>
    /// Used to read in the profile data. Explicitly OptsIn for explicit json properties
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal class LaunchProfileData
    {
        // Well known properties
        const string Prop_commandName = "commandName";
        const string Prop_executablePath = "executablePath";
        const string Prop_commandLineArgs = "commandLineArgs";
        const string Prop_workingDirectory = "workingDirectory";
        const string Prop_launchBrowser = "launchBrowser";
        const string Prop_launchUrl = "launchUrl";
        const string Prop_environmentVariables = "environmentVariables";

        static readonly HashSet<string> KnownProfileProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            {Prop_commandName},
            {Prop_executablePath},
            {Prop_commandLineArgs},
            {Prop_workingDirectory},
            {Prop_launchBrowser},
            {Prop_launchUrl},
            {Prop_environmentVariables},
        };

        public static bool IsKnownProfileProperty(string propertyName)
        {
            return KnownProfileProperties.Contains(propertyName);
        }

        // We don't serialize the name as it the dictionary index
        public string Name { get; set; }
        
        // Or serialize the InMemoryProfile state
        public bool InMemoryProfile { get; set; }

        [JsonProperty(PropertyName = Prop_commandName)]
        public string CommandName { get; set; }

        [JsonProperty(PropertyName = Prop_executablePath)]
        public string ExecutablePath { get; set; }

        [JsonProperty(PropertyName = Prop_commandLineArgs)]
        public string CommandLineArgs{ get; set; }

        [JsonProperty(PropertyName = Prop_workingDirectory)]
        public string WorkingDirectory{ get; set; }

        [JsonProperty(PropertyName = Prop_launchBrowser)]
        public bool?  LaunchBrowser { get; set; }

        [JsonProperty(PropertyName = Prop_launchUrl)]
        public string LaunchUrl { get; set; }

        [JsonProperty(PropertyName=Prop_environmentVariables)]
        public IDictionary<string, string>  EnvironmentVariables { get; set; }

        public IDictionary<string, object>  OtherSettings { get; set; }

        /// <summary>
        /// To handle custom settings, we serialize using LaunchProfileData first and then walk the settings
        /// to pick up other settings. Currently limited to boolean, integer, string and dictionary of string
        /// </summary>
        public static Dictionary<string, LaunchProfileData> DeserializeProfiles(JObject profilesObject)
        {
            var profiles = new Dictionary<string, LaunchProfileData>(StringComparer.Ordinal);

            if (profilesObject == null)
            {
                return profiles;
            }

            // We walk the profilesObject and serialize each subobject component as either a string, or a dictionary<string,string>
            foreach(var profile in profilesObject)
            {
                // Name of profile is the key, value is it's contents. We have specific serializing of the data based on the 
                // JToken type
                LaunchProfileData profileData = JsonConvert.DeserializeObject<LaunchProfileData>(profile.Value.ToString());

                // Now pick up any custom properties. Handle string, int, boolean
                Dictionary<string, object> customSettings = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (var data in profile.Value.Children())
                {
                    JProperty dataProperty = data as JProperty;
                    if (dataProperty == null)
                    {
                        continue;
                    }
                    if (!IsKnownProfileProperty(dataProperty.Name))
                    {
                        try
                        {
                            switch (dataProperty.Value.Type)
                            {
                                case JTokenType.Boolean:
                                    {
                                        bool value = bool.Parse(dataProperty.Value.ToString());
                                        customSettings.Add(dataProperty.Name, value);
                                        break;
                                    }
                                case JTokenType.Integer:
                                    {
                                        int value = int.Parse(dataProperty.Value.ToString());
                                        customSettings.Add(dataProperty.Name, value);
                                        break;
                                    }
                                case JTokenType.Object:
                                    {
                                        var value = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataProperty.Value.ToString());
                                        customSettings.Add(dataProperty.Name, value);
                                        break;
                                    }
                                case JTokenType.String:
                                    {
                                        customSettings.Add(dataProperty.Name, dataProperty.Value.ToString());
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                        }
                        catch 
                        {
                            // TODO: should have message indicating the setting is being ignored. Fix as part of issue
                            //       https://github.com/dotnet/roslyn-project-system/issues/424
                        }
                    }
                }
                
                // Only add custom settings if we actually picked some up
                if(customSettings.Count > 0)
                {
                    profileData.OtherSettings = customSettings;
                }

                profiles.Add(profile.Key, profileData);
            }

            return profiles;
        }

        /// <summary>
        /// Helper to convert an ILaunchProfile back to its serializable form. Bascially, it
        /// converts it to a dictionary of settings. This preserves custom values
        /// </summary>
        public static Dictionary<string, object> ToSerializableForm(ILaunchProfile profile)
        {
            var data = new Dictionary<string, object>(StringComparer.Ordinal);

            // Don't write out empty elements
            if(!string.IsNullOrEmpty(profile.CommandName))
            {
                data.Add(Prop_commandName, profile.CommandName);
            }

            if(!string.IsNullOrEmpty(profile.ExecutablePath))
            {
                data.Add(Prop_executablePath, profile.ExecutablePath);
            }
            
            if(!string.IsNullOrEmpty(profile.CommandLineArgs))
            {
                data.Add(Prop_commandLineArgs, profile.CommandLineArgs);
            }
            
            if(!string.IsNullOrEmpty(profile.WorkingDirectory))
            {
                data.Add(Prop_workingDirectory, profile.WorkingDirectory);
            }
            
            if(profile.LaunchBrowser)
            {
                data.Add(Prop_launchBrowser, profile.LaunchBrowser);
            }
            
            if(!string.IsNullOrEmpty(profile.LaunchUrl))
            {
                data.Add(Prop_launchUrl, profile.LaunchUrl);
            }

            if(profile.EnvironmentVariables != null)
            {
                data.Add(Prop_environmentVariables, profile.EnvironmentVariables);
            }

            if(profile.OtherSettings != null)
            {
                foreach(var kvp in profile.OtherSettings)
                {
                    data.Add(kvp.Key, kvp.Value);
                }
            }

            return data;
        }

        /// <summary>
        /// Helper to convert an ILaunchProfile back to its serializable form. It does some
        /// fixup. Like setting empty values to null.
        /// </summary>
        public static LaunchProfileData FromILaunchProfile(ILaunchProfile profile)
        {
            return new LaunchProfileData()
            {
                Name = profile.Name,
                ExecutablePath = profile.ExecutablePath,
                CommandName = profile.CommandName,
                CommandLineArgs = profile.CommandLineArgs,
                WorkingDirectory = profile.WorkingDirectory,
                LaunchBrowser = profile.LaunchBrowser,
                LaunchUrl= profile.LaunchUrl,
                EnvironmentVariables = profile.EnvironmentVariables,
                OtherSettings = profile.OtherSettings,
                InMemoryProfile = profile.IsInMemoryObject()
            };
        }
    }
}
