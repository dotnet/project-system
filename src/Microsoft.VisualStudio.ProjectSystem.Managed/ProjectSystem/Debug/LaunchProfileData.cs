// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        private const string Prop_commandName = "commandName";
        private const string Prop_executablePath = "executablePath";
        private const string Prop_commandLineArgs = "commandLineArgs";
        private const string Prop_workingDirectory = "workingDirectory";
        private const string Prop_launchBrowser = "launchBrowser";
        private const string Prop_launchUrl = "launchUrl";
        private const string Prop_environmentVariables = "environmentVariables";

        private static readonly HashSet<string> s_knownProfileProperties = new(StringComparers.LaunchProfileProperties)
        {
            Prop_commandName,
            Prop_executablePath,
            Prop_commandLineArgs,
            Prop_workingDirectory,
            Prop_launchBrowser,
            Prop_launchUrl,
            Prop_environmentVariables
        };

        public static bool IsKnownProfileProperty(string propertyName)
        {
            return s_knownProfileProperties.Contains(propertyName);
        }

        // We don't serialize the name as it the dictionary index
        public string? Name { get; set; }

        // Or serialize the InMemoryProfile state
        public bool InMemoryProfile { get; set; }

        [JsonProperty(PropertyName = Prop_commandName)]
        public string? CommandName { get; set; }

        [JsonProperty(PropertyName = Prop_executablePath)]
        public string? ExecutablePath { get; set; }

        [JsonProperty(PropertyName = Prop_commandLineArgs)]
        public string? CommandLineArgs { get; set; }

        [JsonProperty(PropertyName = Prop_workingDirectory)]
        public string? WorkingDirectory { get; set; }

        [JsonProperty(PropertyName = Prop_launchBrowser)]
        public bool? LaunchBrowser { get; set; }

        [JsonProperty(PropertyName = Prop_launchUrl)]
        public string? LaunchUrl { get; set; }

        [JsonProperty(PropertyName = Prop_environmentVariables)]
        public IDictionary<string, string>? EnvironmentVariables { get; set; }

        public IDictionary<string, object>? OtherSettings { get; set; }

        /// <summary>
        /// To handle custom settings, we serialize using LaunchProfileData first and then walk the settings
        /// to pick up other settings. Currently limited to boolean, integer, string and dictionary of string
        /// </summary>
        public static Dictionary<string, LaunchProfileData> DeserializeProfiles(JObject profilesObject)
        {
            var profiles = new Dictionary<string, LaunchProfileData>(StringComparers.LaunchProfileNames);

            if (profilesObject == null)
            {
                return profiles;
            }

            // We walk the profilesObject and serialize each subobject component as either a string, or a dictionary<string,string>
            foreach ((string key, JToken jToken) in profilesObject)
            {
                // Name of profile is the key, value is it's contents. We have specific serializing of the data based on the 
                // JToken type
                LaunchProfileData profileData = JsonConvert.DeserializeObject<LaunchProfileData>(jToken.ToString());

                // Now pick up any custom properties. Handle string, int, boolean
                var customSettings = new Dictionary<string, object>(StringComparers.LaunchProfileProperties);
                foreach (JToken data in jToken.Children())
                {
                    if (data is JProperty property && !IsKnownProfileProperty(property.Name))
                    {
                        try
                        {
                            object? value = property.Value.Type switch
                            {
                                JTokenType.Boolean => bool.Parse(property.Value.ToString()),
                                JTokenType.Integer => int.Parse(property.Value.ToString()),
                                JTokenType.Object => JsonConvert.DeserializeObject<Dictionary<string, string>>(property.Value.ToString()),
                                JTokenType.String => property.Value.ToString(),
                                _ => null
                            };

                            if (value != null)
                            {
                                customSettings.Add(property.Name, value);
                            }
                        }
                        catch
                        {
                            // TODO: should have message indicating the setting is being ignored. Fix as part of issue
                            //       https://github.com/dotnet/project-system/issues/424
                        }
                    }
                }

                // Only add custom settings if we actually picked some up
                if (customSettings.Count > 0)
                {
                    profileData.OtherSettings = customSettings;
                }

                profiles.Add(key, profileData);
            }

            return profiles;
        }

        /// <summary>
        /// Helper to convert an ILaunchProfile back to its serializable form. Basically, it
        /// converts it to a dictionary of settings. This preserves custom values
        /// </summary>
        public static Dictionary<string, object> ToSerializableForm(ILaunchProfile profile)
        {
            var data = new Dictionary<string, object>(StringComparers.LaunchProfileProperties);

            // Don't write out empty elements
            if (!Strings.IsNullOrEmpty(profile.CommandName))
            {
                data.Add(Prop_commandName, profile.CommandName);
            }

            if (!Strings.IsNullOrEmpty(profile.ExecutablePath))
            {
                data.Add(Prop_executablePath, profile.ExecutablePath);
            }

            if (!Strings.IsNullOrEmpty(profile.CommandLineArgs))
            {
                data.Add(Prop_commandLineArgs, profile.CommandLineArgs);
            }

            if (!Strings.IsNullOrEmpty(profile.WorkingDirectory))
            {
                data.Add(Prop_workingDirectory, profile.WorkingDirectory);
            }

            if (profile.LaunchBrowser)
            {
                data.Add(Prop_launchBrowser, profile.LaunchBrowser);
            }

            if (!Strings.IsNullOrEmpty(profile.LaunchUrl))
            {
                data.Add(Prop_launchUrl, profile.LaunchUrl);
            }

            if (profile.EnvironmentVariables != null)
            {
                data.Add(Prop_environmentVariables, profile.EnvironmentVariables);
            }

            if (profile.OtherSettings != null)
            {
                foreach ((string key, object value) in profile.OtherSettings)
                {
                    data.Add(key, value);
                }
            }

            return data;
        }

        /// <summary>
        /// Converts <paramref name="profile"/> to its serializable form.
        /// It does some fix up, like setting empty values to <see langword="null"/>.
        /// </summary>
        public static LaunchProfileData FromILaunchProfile(ILaunchProfile profile)
        {
            var profileData = new LaunchProfileData
            {
                Name = profile.Name,
                ExecutablePath = profile.ExecutablePath,
                CommandName = profile.CommandName,
                CommandLineArgs = profile.CommandLineArgs,
                WorkingDirectory = profile.WorkingDirectory,
                LaunchBrowser = profile.LaunchBrowser,
                LaunchUrl = profile.LaunchUrl,
                EnvironmentVariables = profile.EnvironmentVariables,
                OtherSettings = profile.OtherSettings,
                InMemoryProfile = profile.IsInMemoryObject()
            };

            return profileData;
        }
    }
}
