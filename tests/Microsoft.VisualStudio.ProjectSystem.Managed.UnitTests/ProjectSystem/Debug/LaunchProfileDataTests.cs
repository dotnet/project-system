// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Collections;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchProfileDataTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LaunchProfileData_FromILaunchProfileTests(bool isInMemory)
        {
            var profile = new LaunchProfile()
            {
                Name = "Test",
                CommandName = "Test",
                ExecutablePath = "c:\\this\\is\\a\\exe\\path",
                CommandLineArgs = "args",
                WorkingDirectory = "c:\\working\\directory\\",
                LaunchBrowser = true,
                LaunchUrl = "LaunchPage.html",
                EnvironmentVariables = new Dictionary<string, string>() { { "var1", "Value1" }, { "var2", "Value2" } }.ToImmutableDictionary(),
                OtherSettings = new Dictionary<string, object>(StringComparer.Ordinal) { { "setting1", true }, { "setting2", "mysetting" } }.ToImmutableDictionary(),
                DoNotPersist = isInMemory
            };

            var data = LaunchProfileData.FromILaunchProfile(profile);

            Assert.Equal(data.Name, profile.Name);
            Assert.Equal(data.ExecutablePath, profile.ExecutablePath);
            Assert.Equal(data.CommandLineArgs, profile.CommandLineArgs);
            Assert.Equal(data.WorkingDirectory, profile.WorkingDirectory);
            Assert.Equal(data.LaunchBrowser, profile.LaunchBrowser);
            Assert.Equal(data.LaunchUrl, profile.LaunchUrl);
            Assert.Equal(data.EnvironmentVariables!.ToImmutableDictionary(), profile.EnvironmentVariables, DictionaryEqualityComparer<string, string>.Instance);
            Assert.True(DictionaryEqualityComparer<string, string>.Instance.Equals(data.EnvironmentVariables!.ToImmutableDictionary(), profile.EnvironmentVariables));
            Assert.Equal(isInMemory, data.InMemoryProfile);
        }

        [Fact]
        public void LaunchProfileData_IsKnownPropertyTests()
        {
            Assert.True(LaunchProfileData.IsKnownProfileProperty("commandName"));
            Assert.True(LaunchProfileData.IsKnownProfileProperty("executablePath"));
            Assert.True(LaunchProfileData.IsKnownProfileProperty("commandLineArgs"));
            Assert.True(LaunchProfileData.IsKnownProfileProperty("workingDirectory"));
            Assert.True(LaunchProfileData.IsKnownProfileProperty("launchBrowser"));
            Assert.True(LaunchProfileData.IsKnownProfileProperty("launchUrl"));
            Assert.True(LaunchProfileData.IsKnownProfileProperty("environmentVariables"));
            Assert.False(LaunchProfileData.IsKnownProfileProperty("CommandName"));
            Assert.False(LaunchProfileData.IsKnownProfileProperty("applicationUrl"));
        }

        [Fact]
        public void LaunchProfileData_DeserializeProfilesTests()
        {
            var jsonObject = JObject.Parse(JsonString1);

            var profiles = LaunchProfileData.DeserializeProfiles((JObject)jsonObject["profiles"]);
            Assert.Equal(4, profiles.Count);
            var profile = profiles["IIS Express"];
            Assert.Equal("IISExpress", profile.CommandName);
            Assert.Equal("http://localhost:1234:/test.html", profile.LaunchUrl);
            Assert.True(profile.LaunchBrowser);
            Assert.False(profile.InMemoryProfile);

            profile = profiles["HasCustomValues"];
            Assert.Equal("c:\\test\\project", profile.WorkingDirectory);
            Assert.Equal("c:\\test\\project\\bin\\project.exe", profile.ExecutablePath);
            Assert.Null(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.Null(profile.EnvironmentVariables);
            Assert.NotNull(profile.OtherSettings);
            Assert.True((bool)profile.OtherSettings!["custom1"]);
            Assert.Equal(124, profile.OtherSettings["custom2"]);
            Assert.Equal("mycustomVal", profile.OtherSettings["custom3"]);
            Assert.False(profile.InMemoryProfile);

            profile = profiles["Docker"];
            Assert.Equal("Docker", profile.CommandName);
            Assert.Null(profile.WorkingDirectory);
            Assert.Null(profile.ExecutablePath);
            Assert.False(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.Null(profile.EnvironmentVariables);
            Assert.NotNull(profile.OtherSettings);
            Assert.Equal("some option in docker", profile.OtherSettings!["dockerOption1"]);
            Assert.Equal("Another option in docker", profile.OtherSettings["dockerOption2"]);
            Assert.False(profile.InMemoryProfile);

            profile = profiles["web"];
            Assert.Equal("Project", profile.CommandName);
            Assert.Null(profile.WorkingDirectory);
            Assert.Null(profile.ExecutablePath);
            Assert.True(profile.LaunchBrowser);
            Assert.Null(profile.LaunchUrl);
            Assert.NotNull(profile.EnvironmentVariables);
            Assert.Equal("Development", profile.EnvironmentVariables!["ASPNET_ENVIRONMENT"]);
            Assert.Equal("c:\\Users\\billhie\\Documents\\projects\\WebApplication8\\src\\WebApplication8", profile.EnvironmentVariables["ASPNET_APPLICATIONBASE"]);
            Assert.Null(profile.OtherSettings);
            Assert.False(profile.InMemoryProfile);
        }

        [Fact]
        public void LaunchProfileData_DeserializeEmptyProfilesTests()
        {
            var jsonObject = JObject.Parse(JsonString2);
            var profiles = LaunchProfileData.DeserializeProfiles((JObject)jsonObject["profiles"]);
            Assert.Empty(profiles);
        }

        [Fact]
        public void LaunchProfileData_ToSerializableFormTests()
        {
            var jsonObject = JObject.Parse(JsonString1);
            var profiles = LaunchProfileData.DeserializeProfiles((JObject)jsonObject["profiles"]);

            var profile = profiles["IIS Express"];
            var serializableProfile = LaunchProfileData.ToSerializableForm(new LaunchProfile(profile));

            AssertEx.CollectionLength(serializableProfile, 3);
            Assert.Equal("IISExpress", serializableProfile["commandName"]);
            Assert.Equal("http://localhost:1234:/test.html", serializableProfile["launchUrl"]);
            Assert.True((bool)serializableProfile["launchBrowser"]);

            profile = profiles["HasCustomValues"];
            serializableProfile = LaunchProfileData.ToSerializableForm(new LaunchProfile(profile));
            Assert.Equal(6, serializableProfile.Count);
            Assert.Equal("c:\\test\\project", serializableProfile["workingDirectory"]);
            Assert.Equal("c:\\test\\project\\bin\\project.exe", serializableProfile["executablePath"]);
            Assert.Equal("--arg1 --arg2", serializableProfile["commandLineArgs"]);
            Assert.True((bool)serializableProfile["custom1"]);
            Assert.Equal(124, serializableProfile["custom2"]);
            Assert.Equal("mycustomVal", serializableProfile["custom3"]);

            // tests launchBrowser:false is not rewritten
            profile = profiles["Docker"];
            serializableProfile = LaunchProfileData.ToSerializableForm(new LaunchProfile(profile));
            AssertEx.CollectionLength(serializableProfile, 3);
            Assert.Equal("Docker", serializableProfile["commandName"]);
            Assert.Equal("some option in docker", serializableProfile["dockerOption1"]);
            Assert.Equal("Another option in docker", serializableProfile["dockerOption2"]);

            profile = profiles["web"];
            serializableProfile = LaunchProfileData.ToSerializableForm(new LaunchProfile(profile));
            AssertEx.CollectionLength(serializableProfile, 3);
            Assert.Equal("Project", serializableProfile["commandName"]);
            Assert.True((bool)serializableProfile["launchBrowser"]);
            Assert.Equal("Development", ((IDictionary)serializableProfile["environmentVariables"])["ASPNET_ENVIRONMENT"]);
            Assert.Equal("c:\\Users\\billhie\\Documents\\projects\\WebApplication8\\src\\WebApplication8", ((IDictionary)serializableProfile["environmentVariables"])["ASPNET_APPLICATIONBASE"]);
        }

        // Json string data
        private readonly string JsonString1 = @"{
  ""profiles"": {
  ""IIS Express"" :
    {
      ""commandName"": ""IISExpress"",
      ""launchUrl"": ""http://localhost:1234:/test.html"",
      ""launchBrowser"": true
    },
    ""HasCustomValues"" :
    {
      ""executablePath"": ""c:\\test\\project\\bin\\project.exe"",
      ""workingDirectory"": ""c:\\test\\project"",
      ""commandLineArgs"": ""--arg1 --arg2"",
      ""custom1"" : true,
      ""custom2"" : 124,
      ""custom3"" : ""mycustomVal""
    },
    ""Docker"" :
    {
      ""commandName"": ""Docker"",
      ""launchBrowser"": false,
      ""dockerOption1"" : ""some option in docker"",
      ""dockerOption2"" : ""Another option in docker""
    },
    ""web"" :
    {
      ""commandName"": ""Project"",
      ""launchBrowser"": true,
      ""environmentVariables"": {
        ""ASPNET_ENVIRONMENT"": ""Development"",
        ""ASPNET_APPLICATIONBASE"": ""c:\\Users\\billhie\\Documents\\projects\\WebApplication8\\src\\WebApplication8""
      }
    }
  }
}";
        private readonly string JsonString2 = @"{
  ""profiles"": {
  }
}";
    }
}
