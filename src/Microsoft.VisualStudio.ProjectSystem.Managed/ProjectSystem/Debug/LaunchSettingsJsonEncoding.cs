// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

/// <summary>
/// Responsible for encoding and decoding launch profile data in JSON format.
/// </summary>
internal static class LaunchSettingsJsonEncoding
{
    /// <summary>
    /// The name of the JSON property containing the set of profiles.
    /// Profiles are stored in a JSON object, where each property's name
    /// is the profile name, and value is the profile itself.
    /// </summary>
    private const string ProfilesSectionName = "profiles";

    private const string CommandNamePropertyName = "commandName";
    private const string ExecutablePathPropertyName = "executablePath";
    private const string CommandLineArgsPropertyName = "commandLineArgs";
    private const string WorkingDirectoryPropertyName = "workingDirectory";
    private const string LaunchBrowserPropertyName = "launchBrowser";
    private const string LaunchUrlPropertyName = "launchUrl";
    private const string EnvironmentVariablesPropertyName = "environmentVariables";

    /// <summary>
    /// Formats <paramref name="settings"/> as a JSON string.
    /// </summary>
    /// <param name="settings">The settings to serialize.</param>
    /// <returns>The serialized settings data, in JSON.</returns>
    public static string ToJson(ILaunchSettings settings)
    {
        StringWriter sw = new();

        using JsonTextWriter writer = new(sw) { Formatting = Formatting.Indented };

        writer.WriteStartObject();
        {
            WriteSettings(writer, settings);
        }
        writer.WriteEndObject();

        return sw.ToString();

        static void WriteSettings(JsonWriter writer, ILaunchSettings settings)
        {
            bool startedProfiles = false;

            foreach (ILaunchProfile profile in settings.Profiles)
            {
                if (!profile.IsInMemoryObject())
                {
                    Assumes.NotNull(profile.Name);
                    EnsureStartedProfiles();
                    writer.WritePropertyName(profile.Name);
                    WriteLaunchProfile(profile);
                }
            }

            if (startedProfiles)
            {
                // End the profiles object.
                writer.WriteEndObject();
            }

            JsonSerializer? jsonSerializer = null;
            
            foreach ((string key, object value) in settings.GlobalSettings)
            {
                if (!value.IsInMemoryObject())
                {
                    writer.WritePropertyName(key);
                    if (value is string s)
                        writer.WriteValue(s);
                    if (value is int i)
                        writer.WriteValue(i);
                    if (value is null)
                        writer.WriteNull();
                    if (value is bool b)
                        writer.WriteValue(b);
                    else
                    {
                        jsonSerializer ??= JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        jsonSerializer.Serialize(writer, value);
                    }
                }
            }

            return;

            void EnsureStartedProfiles()
            {
                if (startedProfiles)
                    return;
                startedProfiles = true;

                writer.WritePropertyName(ProfilesSectionName);

                // Start the profiles object.
                writer.WriteStartObject();
            }

            void WriteLaunchProfile(ILaunchProfile profile)
            {
                // Don't write out empty elements

                writer.WriteStartObject();

                if (!Strings.IsNullOrEmpty(profile.CommandName))
                {
                    writer.WritePropertyName(CommandNamePropertyName);
                    writer.WriteValue(profile.CommandName);
                }

                if (!Strings.IsNullOrEmpty(profile.ExecutablePath))
                {
                    writer.WritePropertyName(ExecutablePathPropertyName);
                    writer.WriteValue(profile.ExecutablePath);
                }

                if (!Strings.IsNullOrEmpty(profile.CommandLineArgs))
                {
                    writer.WritePropertyName(CommandLineArgsPropertyName);
                    writer.WriteValue(profile.CommandLineArgs);
                }

                if (!Strings.IsNullOrEmpty(profile.WorkingDirectory))
                {
                    writer.WritePropertyName(WorkingDirectoryPropertyName);
                    writer.WriteValue(profile.WorkingDirectory);
                }

                if (profile.LaunchBrowser)
                {
                    writer.WritePropertyName(LaunchBrowserPropertyName);
                    writer.WriteValue(profile.LaunchBrowser);
                }

                if (!Strings.IsNullOrEmpty(profile.LaunchUrl))
                {
                    writer.WritePropertyName(LaunchUrlPropertyName);
                    writer.WriteValue(profile.LaunchUrl);
                }

                ImmutableArray<(string Key, string Value)> vars = profile.FlattenEnvironmentVariables();

                if (!vars.IsEmpty)
                {
                    writer.WritePropertyName(EnvironmentVariablesPropertyName);

                    writer.WriteStartObject();
                    {
                        foreach ((string key, string value) in vars)
                        {
                            writer.WritePropertyName(key);
                            writer.WriteValue(value);
                        }
                    }
                    writer.WriteEndObject();
                }

                foreach ((string key, object value) in profile.EnumerateOtherSettings())
                {
                    writer.WritePropertyName(key);
                    writer.WriteValue(value);
                }

                writer.WriteEndObject();
            }
        }
    }

    /// <summary>
    /// Parses launch profile data from a JSON string.
    /// </summary>
    /// <param name="json">The source JSON text to read from.</param>
    /// <param name="jsonSerializationProviders">Providers for custom serialisation of other settings. May be empty.</param>
    /// <returns>Deserialized launch settings.</returns>
    /// <exception cref="FormatException">JSON data was not of the expected format.</exception>
    public static LaunchSettingsData FromJson(string json, OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection> jsonSerializationProviders)
    {
        JsonSerializer? jsonSerializer = null;

        using JsonTextReader reader = new(new StringReader(json));

        LaunchSettingsData data = new();
        data.Profiles ??= new();

        ReadObject(property =>
        {
            if (string.IsNullOrWhiteSpace(property))
            {
                return;
            }
            else if (property == ProfilesSectionName)
            {
                ReadObject(profileName => data.Profiles.Add(ReadProfile(profileName)));
            }
            else if (ReadOtherSettingValue(property) is { } value)
            {
                data.OtherSettings ??= new(StringComparers.LaunchSettingsPropertyNames);
                data.OtherSettings.Add(property, value);
            }
        });

        return data;

        LaunchProfileData ReadProfile(string name)
        {
            var data = new LaunchProfileData { Name = name };

            ReadObject(property =>
            {
                switch (property)
                {
                    case CommandNamePropertyName:
                    {
                        data.CommandName = ReadString();
                        break;
                    }
                    case ExecutablePathPropertyName:
                    {
                        data.ExecutablePath = ReadString();
                        break;
                    }
                    case CommandLineArgsPropertyName:
                    {
                        data.CommandLineArgs = ReadString();
                        break;
                    }
                    case WorkingDirectoryPropertyName:
                    {
                        data.WorkingDirectory = ReadString();
                        break;
                    }
                    case LaunchBrowserPropertyName:
                    {
                        data.LaunchBrowser = ReadBoolean();
                        break;
                    }
                    case LaunchUrlPropertyName:
                    {
                        data.LaunchUrl = ReadString();
                        break;
                    }
                    case EnvironmentVariablesPropertyName:
                    {
                        data.EnvironmentVariables = ReadEnvironmentVariables();
                        break;
                    }
                    default:
                    {
                        object? value = ReadCustomProfileSetting(property);
                        if (value is not null)
                        {
                            data.OtherSettings ??= new(StringComparers.LaunchSettingsPropertyNames);
                            data.OtherSettings[property] = value;
                        }
                        break;
                    }
                }
            });

            return data;
        }

        object? ReadCustomProfileSetting(string propertyName)
        {
            if (!reader.Read())
                return null;

            try
            {
                return reader.TokenType switch
                {
                    JsonToken.Boolean => (bool)reader.Value!,
                    JsonToken.Integer => checked((int)(long)reader.Value!),
                    JsonToken.StartObject => (jsonSerializer ??= JsonSerializer.CreateDefault()).Deserialize<Dictionary<string, string>>(reader),
                    JsonToken.String => (string)reader.Value!,
                    _ => null
                };
            }
            catch
            {
                // TODO: should have message indicating the setting is being ignored. Fix as part of issue
                //       https://github.com/dotnet/project-system/issues/424
                return null;
            }
        }

        object? ReadOtherSettingValue(string property)
        {
            // Look for a handler that's registered for this section.
            // Note that these handlers are never instantiated, and never perform any "handling".
            // They just expose MEF metadata that controls how settings should be interpreted.
            Lazy<ILaunchSettingsSerializationProvider, IJsonSection>? handler = jsonSerializationProviders
                .FirstOrDefault(sp => StringComparers.LaunchSettingsPropertyNames.Equals(sp.Metadata.JsonSection, property));

            // Move the reader past the PropertyName token, as both JToken.ReadFrom and
            // JsonSerializer.Deserialize start from the current token, not the next token.
            if (!reader.Read())
                throw new FormatException($"Unexpected end of JSON data at {reader.LineNumber}:{reader.LinePosition}.");

            if (handler != null)
            {
                jsonSerializer ??= JsonSerializer.CreateDefault();
                
                // Deserialize using the provider's requested type.
                return jsonSerializer.Deserialize(reader, handler.Metadata.SerializationType);
            }
            else
            {
                // We still need to remember settings for which we don't have an extensibility component installed.
                // Keep the token, which can be serialized back out when the file is written.
                
                // Return the raw JSON token object.
                return JToken.ReadFrom(reader);
            }
        }

        Dictionary<string, string>? ReadEnvironmentVariables()
        {
            Dictionary<string, string>? data = null;

            ReadObject(property =>
            {
                data ??= new(StringComparers.EnvironmentVariableNames);
                data[property] = ReadString();
            });

            return data;
        }

        string ReadString()
        {
            if (!reader.Read() || reader.TokenType != JsonToken.String)
                throw new FormatException($"Expected string at {reader.LineNumber}:{reader.LinePosition} but got {reader.TokenType}.");
            return (string)reader.Value!;
        }

        bool ReadBoolean()
        {
            if (!reader.Read() || reader.TokenType != JsonToken.Boolean)
                throw new FormatException($"Expected bool at {reader.LineNumber}:{reader.LinePosition} but got {reader.TokenType}.");
            return (bool)reader.Value!;
        }

        void ReadObject(Action<string> callback)
        {
            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
                throw new FormatException($"Expected object start at {reader.LineNumber}:{reader.LinePosition} but got {reader.TokenType}.");

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType != JsonToken.PropertyName)
                    throw new FormatException($"Expected property name at {reader.LineNumber}:{reader.LinePosition} but got {reader.TokenType}.");

                string propertyName = (string)reader.Value!;

                callback(propertyName);
            }
        }
    }
}
