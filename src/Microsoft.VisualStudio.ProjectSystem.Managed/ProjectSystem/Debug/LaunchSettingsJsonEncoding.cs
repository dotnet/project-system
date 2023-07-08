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
    /// Formats the specified launch settings data as a JSON string.
    /// </summary>
    /// <param name="profiles">The set of profiles to write to JSON.</param>
    /// <param name="globalSettings">The set of global settings to write to JSON.</param>
    /// <returns>The serialized settings data, in JSON.</returns>
    public static string ToJson(IEnumerable<ILaunchProfile> profiles, IEnumerable<(string Key, object Value)> globalSettings)
    {
        StringWriter sw = new();

        using JsonTextWriter writer = new(sw) { Formatting = Formatting.Indented };

        writer.WriteStartObject();
        {
            WriteSettings();
        }
        writer.WriteEndObject();

        return sw.ToString();

        void WriteSettings()
        {
            bool startedProfiles = false;
            JsonSerializer? jsonSerializer = null;

            foreach (ILaunchProfile profile in profiles)
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
            
            foreach ((string key, object value) in globalSettings)
            {
                if (!value.IsInMemoryObject())
                {
                    writer.WritePropertyName(key);
                    WriteObject(value);
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
                    WriteObject(value);
                }

                writer.WriteEndObject();
            }

            void WriteObject(object value)
            {
                if (value is string s)
                    writer.WriteValue(s);
                else if (value is int i)
                    writer.WriteValue(i);
                else if (value is null)
                    writer.WriteNull();
                else if (value is bool b)
                    writer.WriteValue(b);
                else
                {
                    jsonSerializer ??= JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    jsonSerializer.Serialize(writer, value);
                }
            }
        }
    }

    /// <summary>
    /// Parses launch profile data from a JSON string.
    /// </summary>
    /// <param name="json">The source JSON text to read from.</param>
    /// <param name="jsonSerializationProviders">Providers for custom serialisation of other settings. May be empty.</param>
    /// <returns>Deserialized launch settings.</returns>
    /// <exception cref="JsonReaderException">JSON data was not of the expected format.</exception>
    public static (ImmutableArray<LaunchProfile> Profiles, ImmutableArray<(string Name, object Value)> GlobalSettings) FromJson(
        TextReader json,
        OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection> jsonSerializationProviders)
    {
        JsonSerializer? jsonSerializer = null;

        using CommentSkippingJsonTextReader reader = new(json);

        ImmutableArray<LaunchProfile>.Builder? profiles = null;
        ImmutableArray<(string Name, object Value)>.Builder? otherSettings = null;

        ReadObject(property =>
        {
            if (string.IsNullOrWhiteSpace(property))
            {
                return;
            }
            else if (property == ProfilesSectionName)
            {
                ReadObject(profileName =>
                {
                    profiles ??= ImmutableArray.CreateBuilder<LaunchProfile>();
                    profiles.Add(ReadProfile(profileName));
                });
            }
            else if (ReadOtherSettingValue(property) is { } value)
            {
                otherSettings ??= ImmutableArray.CreateBuilder<(string Name, object Value)>();
                otherSettings.Add((property, value));
            }
        });

        return (
            profiles?.ToImmutable() ?? ImmutableArray<LaunchProfile>.Empty,
            otherSettings?.ToImmutable() ?? ImmutableArray<(string Name, object Value)>.Empty);

        LaunchProfile ReadProfile(string name)
        {
            string? commandName = null;
            string? executablePath = null;
            string? commandLineArgs = null;
            string? workingDirectory = null;
            bool launchBrowser = false;
            string? launchUrl = null;
            ImmutableArray<(string Name, string Value)> environmentVariables = ImmutableArray<(string Name, string Value)>.Empty;
            ImmutableArray<(string Name, object Value)>.Builder? otherSettings = null;

            ReadObject(property =>
            {
                switch (property)
                {
                    case CommandNamePropertyName:
                    {
                        commandName = ReadString();
                        break;
                    }
                    case ExecutablePathPropertyName:
                    {
                        executablePath = ReadString();
                        break;
                    }
                    case CommandLineArgsPropertyName:
                    {
                        commandLineArgs = ReadString();
                        break;
                    }
                    case WorkingDirectoryPropertyName:
                    {
                        workingDirectory = ReadString();
                        break;
                    }
                    case LaunchBrowserPropertyName:
                    {
                        launchBrowser = ReadBoolean();
                        break;
                    }
                    case LaunchUrlPropertyName:
                    {
                        launchUrl = ReadString();
                        break;
                    }
                    case EnvironmentVariablesPropertyName:
                    {
                        environmentVariables = ReadEnvironmentVariables();
                        break;
                    }
                    default:
                    {
                        object? value = ReadCustomProfileSetting(property);
                        if (value is not null)
                        {
                            otherSettings ??= ImmutableArray.CreateBuilder<(string Name, object Value)>();
                            otherSettings.Add((property, value));
                        }
                        break;
                    }
                }
            });

            return new LaunchProfile(
                name: name,
                commandName: commandName,
                executablePath: executablePath,
                commandLineArgs: commandLineArgs,
                workingDirectory: workingDirectory,
                launchBrowser: launchBrowser,
                launchUrl: launchUrl,
                environmentVariables: environmentVariables,
                otherSettings: otherSettings?.ToImmutable() ?? ImmutableArray<(string Name, object Value)>.Empty);
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
                    JsonToken.StartObject => (jsonSerializer ??= JsonSerializer.CreateDefault()).Deserialize<Dictionary<string, object>>(reader),
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
                throw new JsonReaderException($"Unexpected end of JSON data at {reader.LineNumber}:{reader.LinePosition}.");

            if (handler is not null)
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

        ImmutableArray<(string Name, string Value)> ReadEnvironmentVariables()
        {
            ImmutableArray<(string Name, string Value)>.Builder? builder = null;

            ReadObject(property =>
            {
                builder ??= ImmutableArray.CreateBuilder<(string Name, string Value)>();
                builder.Add((property, ReadString()));
            });

            return builder?.ToImmutable() ?? ImmutableArray<(string Name, string Value)>.Empty;
        }

        string ReadString()
        {
            if (!reader.Read() || reader.TokenType != JsonToken.String)
                throw new JsonReaderException($"Expected string at {reader.LineNumber}:{reader.LinePosition} but got {reader.TokenType}.");
            return (string)reader.Value!;
        }

        bool ReadBoolean()
        {
            if (!reader.Read() || reader.TokenType != JsonToken.Boolean)
                throw new JsonReaderException($"Expected bool at {reader.LineNumber}:{reader.LinePosition} but got {reader.TokenType}.");
            return (bool)reader.Value!;
        }

        void ReadObject(Action<string> callback)
        {
            if (!reader.Read() || reader.TokenType != JsonToken.StartObject)
                throw new JsonReaderException($"Expected object start at {reader.LineNumber}:{reader.LinePosition} but got {reader.TokenType}.");

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType != JsonToken.PropertyName)
                    throw new JsonReaderException($"Expected property name at {reader.LineNumber}:{reader.LinePosition} but got {reader.TokenType}.");

                string propertyName = (string)reader.Value!;

                callback(propertyName);
            }
        }
    }

    /// <summary>
    /// An implementation of <see cref="JsonTextReader"/> that skips any comments found
    /// in the JSON data.
    /// </summary>
    private sealed class CommentSkippingJsonTextReader : JsonTextReader
    {
        public CommentSkippingJsonTextReader(TextReader reader) : base(reader)
        {
            DateParseHandling = DateParseHandling.None;
        }

        public override bool Read()
        {
            while (true)
            {
                if (!base.Read())
                    return false;
                if (TokenType != JsonToken.Comment)
                    return true;
            }
        }
    }
}
