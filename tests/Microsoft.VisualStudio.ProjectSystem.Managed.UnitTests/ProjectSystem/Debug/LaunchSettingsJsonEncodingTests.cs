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
            var settings = LaunchSettingsJsonEncoding.FromJson(json, null!);

            Assert.NotNull(settings.Profiles);
            Assert.Empty(settings.Profiles);

            Assert.Null(settings.OtherSettings);
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

            var settings = LaunchSettingsJsonEncoding.FromJson(json, _providers);

            List<LaunchProfileData>? profiles = settings.Profiles;
            Assert.NotNull(profiles);
            Assert.Equal(4, profiles.Count);

            var profile = profiles[0];
            Assert.Equal("IIS Express", profile.Name);
            Assert.Equal("IISExpress", profile.CommandName);
            Assert.Equal("http://localhost:1234/test.html", profile.LaunchUrl);
            Assert.True(profile.LaunchBrowser);
            Assert.False(profile.InMemoryProfile);

            profile = profiles[1];
            Assert.Equal("HasCustomValues", profile.Name);
            Assert.Equal("c:\\test\\project", profile.WorkingDirectory);
            Assert.Equal("c:\\test\\project\\bin\\project.exe", profile.ExecutablePath);
            Assert.False(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.Null(profile.EnvironmentVariables);
            Assert.NotNull(profile.OtherSettings);
            Assert.True((bool)profile.OtherSettings["custom1"]);
            Assert.Equal(124, profile.OtherSettings["custom2"]);
            Assert.Equal("mycustomVal", profile.OtherSettings["custom3"]);
            Assert.False(profile.InMemoryProfile);

            profile = profiles[2];
            Assert.Equal("Docker", profile.Name);
            Assert.Equal("Docker", profile.CommandName);
            Assert.Null(profile.WorkingDirectory);
            Assert.Null(profile.ExecutablePath);
            Assert.False(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.Null(profile.EnvironmentVariables);
            Assert.NotNull(profile.OtherSettings);
            Assert.Equal("some option in docker", profile.OtherSettings["dockerOption1"]);
            Assert.Equal("Another option in docker", profile.OtherSettings["dockerOption2"]);
            Assert.False(profile.InMemoryProfile);

            profile = profiles[3];
            Assert.Equal("web", profile.Name);
            Assert.Equal("Project", profile.CommandName);
            Assert.Null(profile.WorkingDirectory);
            Assert.Null(profile.ExecutablePath);
            Assert.True(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.NotNull(profile.EnvironmentVariables);
            Assert.Equal("Development", profile.EnvironmentVariables["ASPNET_ENVIRONMENT"]);
            Assert.Equal("c:\\Users\\billhie\\Documents\\projects\\WebApplication8\\src\\WebApplication8", profile.EnvironmentVariables["ASPNET_APPLICATIONBASE"]);
            Assert.Null(profile.OtherSettings);
            Assert.False(profile.InMemoryProfile);

            var roundTrippedJson = LaunchSettingsJsonEncoding.ToJson(new LaunchSettings(settings, null, 0));

            Assert.Equal(json, roundTrippedJson);
        }
    }
}
