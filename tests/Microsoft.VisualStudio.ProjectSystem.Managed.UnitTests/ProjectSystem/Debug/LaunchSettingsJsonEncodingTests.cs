// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public sealed class LaunchSettingsJsonEncodingTests
    {
        private readonly OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection> _providers = new(projectCapabilityCheckProvider: (UnconfiguredProject?)null);

        [Theory]
        [InlineData("""{ }""")]
        [InlineData("""{ "profiles": {} }""")]
        public void FromJson_Empty(string json)
        {
            var (profiles, globalSettings) = LaunchSettingsJsonEncoding.FromJson(new StringReader(json), null!);

            Assert.Empty(profiles);
            Assert.Empty(globalSettings);
        }

        [Fact]
        public void RoundTripMultipleProfiles()
        {
            const string json =
            """
            {
              "profiles": {
                "IIS Express": {
                  "commandName": "IISExpress",
                  "launchBrowser": true,
                  "launchUrl": "http://localhost:1234/test.html"
                },
                "HasCustomValues": {
                  "executablePath": "c:\\test\\project\\bin\\project.exe",
                  "commandLineArgs": "--arg1 --arg2",
                  "workingDirectory": "c:\\test\\project",
                  "custom1": true,
                  "custom2": 124,
                  "custom3": "mycustomVal"
                },
                "Docker": {
                  "commandName": "Docker",
                  "dockerOption1": "some option in docker",
                  "dockerOption2": "Another option in docker"
                },
                "web": {
                  "commandName": "Project",
                  "launchBrowser": true,
                  "environmentVariables": {
                    "ASPNET_ENVIRONMENT": "Development",
                    "ASPNET_APPLICATIONBASE": "c:\\Users\\billhie\\Documents\\projects\\WebApplication8\\src\\WebApplication8"
                  }
                },
                "Docker Compose": {
                  "commandName": "DockerCompose",
                  "commandVersion": "1.0",
                  "composeProfile": {
                    "includes": [
                      "web1"
                    ]
                  }
                },
                "DateInEnvironmentVariableBugRepro": {
                  "commandName": "Project",
                  "environmentVariables": {
                    "DATE": "2019-07-11T12:00:00"
                  }
                }
              },
              "string": "hello",
              "int": 123,
              "bool": true,
              "null": null,
              "dictionary": {
                "A": "1",
                "B": "2"
              }
            }
            """;

            var (profiles, globalSettings) = LaunchSettingsJsonEncoding.FromJson(new StringReader(json), _providers);

            Assert.Equal(6, profiles.Length);

            var profile = profiles[0];
            Assert.Equal("IIS Express", profile.Name);
            Assert.Equal("IISExpress", profile.CommandName);
            Assert.Equal("http://localhost:1234/test.html", profile.LaunchUrl);
            Assert.True(profile.LaunchBrowser);
            Assert.False(profile.IsInMemoryObject());

            profile = profiles[1];
            Assert.Equal("HasCustomValues", profile.Name);
            Assert.Equal("c:\\test\\project", profile.WorkingDirectory);
            Assert.Equal("c:\\test\\project\\bin\\project.exe", profile.ExecutablePath);
            Assert.False(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.Equal(3, profile.OtherSettings.Length);
            Assert.Equal(("custom1", true), profile.OtherSettings[0]);
            Assert.Equal(("custom2", 124), profile.OtherSettings[1]);
            Assert.Equal(("custom3", "mycustomVal"), profile.OtherSettings[2]);
            Assert.False(profile.IsInMemoryObject());

            profile = profiles[2];
            Assert.Equal("Docker", profile.Name);
            Assert.Equal("Docker", profile.CommandName);
            Assert.Null(profile.WorkingDirectory);
            Assert.Null(profile.ExecutablePath);
            Assert.False(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.Empty(profile.EnvironmentVariables);
            Assert.Equal(2, profile.OtherSettings.Length);
            Assert.Equal(("dockerOption1", "some option in docker"), profile.OtherSettings[0]);
            Assert.Equal(("dockerOption2", "Another option in docker"), profile.OtherSettings[1]);
            Assert.False(profile.IsInMemoryObject());

            profile = profiles[3];
            Assert.Equal("web", profile.Name);
            Assert.Equal("Project", profile.CommandName);
            Assert.Null(profile.WorkingDirectory);
            Assert.Null(profile.ExecutablePath);
            Assert.True(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.Equal(2, profile.EnvironmentVariables.Length);
            Assert.Equal(("ASPNET_ENVIRONMENT", "Development"), profile.EnvironmentVariables[0]);
            Assert.Equal(("ASPNET_APPLICATIONBASE", @"c:\Users\billhie\Documents\projects\WebApplication8\src\WebApplication8"), profile.EnvironmentVariables[1]);
            Assert.False(profile.IsInMemoryObject());

            profile = profiles[4];
            Assert.Equal("Docker Compose", profile.Name);
            Assert.Equal("DockerCompose", profile.CommandName);
            Assert.Null(profile.WorkingDirectory);
            Assert.Null(profile.ExecutablePath);
            Assert.False(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.Empty(profile.EnvironmentVariables);
            Assert.Equal(2, profile.OtherSettings.Length);
            Assert.Equal("commandVersion", profile.OtherSettings[0].Key);
            Assert.Equal("1.0", profile.OtherSettings[0].Value);
            Assert.Equal("composeProfile", profile.OtherSettings[1].Key);
            var composeProfiles = Assert.IsType<Dictionary<string, object>>(profile.OtherSettings[1].Value);
            var includes = Assert.IsType<JArray>(composeProfiles["includes"]);
            Assert.Single(includes);
            Assert.Equal("web1", includes[0]);
            Assert.False(profile.IsInMemoryObject());

            Assert.Equal(5, globalSettings.Length);
            Assert.Equal(("string", new JValue("hello")), globalSettings[0]);
            Assert.Equal(("int", new JValue(123)), globalSettings[1]);
            Assert.Equal(("bool", new JValue(true)), globalSettings[2]);
            Assert.Equal(("null", JValue.CreateNull()), globalSettings[3]);
            var dicObj = Assert.IsType<JObject>(globalSettings[4].Value);
            Assert.Equal(2, dicObj.Count);
            Assert.Equal("1", dicObj["A"]);
            Assert.Equal("2", dicObj["B"]);

            profile = profiles[5];
            Assert.Equal("DateInEnvironmentVariableBugRepro", profile.Name);
            Assert.Equal("Project", profile.CommandName);
            Assert.Null(profile.WorkingDirectory);
            Assert.Null(profile.ExecutablePath);
            Assert.False(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.Equal(("DATE", "2019-07-11T12:00:00"), Assert.Single(profile.EnvironmentVariables));
            Assert.False(profile.IsInMemoryObject());

            var roundTrippedJson = LaunchSettingsJsonEncoding.ToJson(profiles, globalSettings);

            Assert.Equal(json, roundTrippedJson);
        }

        [Fact]
        public void ToJson_HandlesComplexOtherAndGlobalSettings()
        {
            var settings = ImmutableArray.Create<(string Key, object Value)>(
                ("string", "hello"),
                ("int", 123),
                ("bool", true),
                ("null", null!),
                ("dictionary", new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" }));

            var launchProfile = new LaunchProfile(
                "Name",
                "Command",
                otherSettings: settings);

            var actual = LaunchSettingsJsonEncoding.ToJson(new[] { launchProfile }, globalSettings: settings);

            var expected =
                """
                {
                  "profiles": {
                    "Name": {
                      "commandName": "Command",
                      "string": "hello",
                      "int": 123,
                      "bool": true,
                      "null": null,
                      "dictionary": {
                        "A": "1",
                        "B": "2"
                      }
                    }
                  },
                  "string": "hello",
                  "int": 123,
                  "bool": true,
                  "null": null,
                  "dictionary": {
                    "A": "1",
                    "B": "2"
                  }
                }
                """;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FromJson_WithComments()
        {
            // https://github.com/dotnet/project-system/issues/8168
            const string json =
            """
            {
              "profiles": {
                "ConsoleApp1": {
                  "commandName": "Project"
                  //"commandLineArgs": "1111"
                }
              }
            }
            """;

            var (profiles, globalSettings) = LaunchSettingsJsonEncoding.FromJson(new StringReader(json), _providers);

            var profile = Assert.Single(profiles);

            Assert.Equal("ConsoleApp1", profile.Name);
            Assert.Equal("Project", profile.CommandName);
            Assert.False(profile.DoNotPersist);
            Assert.Null(profile.ExecutablePath);
            Assert.Null(profile.CommandLineArgs);
            Assert.Null(profile.WorkingDirectory);
            Assert.Null(profile.LaunchUrl);
            Assert.False(profile.LaunchBrowser);
            Assert.Empty(profile.OtherSettings);
            Assert.False(profile.IsInMemoryObject());

            Assert.Empty(globalSettings);
        }
    }
}
