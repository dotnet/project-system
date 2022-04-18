// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
            var (profiles, globalSettings) = LaunchSettingsJsonEncoding.FromJson(json, null!);

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
                }
              }
            }
            """;

            var (profiles, globalSettings) = LaunchSettingsJsonEncoding.FromJson(json, _providers);

            Assert.Equal(4, profiles.Length);

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

            var roundTrippedJson = LaunchSettingsJsonEncoding.ToJson(profiles, globalSettings);

            Assert.Equal(json, roundTrippedJson);
        }
    }
}
