// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Collections;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    [ProjectSystemTrait]
    public class LaunchProfileDataTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LaunchProfileData_FromILaunchProfileTests(bool isInMemory)
        {
            LaunchProfile profile = new LaunchProfile()
            {
                Name = "Test",
                CommandName = "Test",
                ExecutablePath = "c:\\this\\is\\a\\exe\\path",
                CommandLineArgs = "args",
                WorkingDirectory = "c:\\wprking\\directory\\",
                LaunchBrowser = true,
                LaunchUrl = "LaunchPage.html",
                EnvironmentVariables = new Dictionary<string, string>() { { "var1", "Value1" }, { "var2", "Value2" } }.ToImmutableDictionary(),
                OtherSettings = new Dictionary<string, object>(StringComparer.Ordinal) { { "setting1", true }, { "setting2", "mysetting" } }.ToImmutableDictionary(),
                DoNotPersist = isInMemory
            };

            LaunchProfileData data = LaunchProfileData.FromILaunchProfile(profile);

            Assert.True(data.Name == profile.Name);
            Assert.True(data.ExecutablePath == profile.ExecutablePath);
            Assert.True(data.CommandLineArgs == profile.CommandLineArgs);
            Assert.True(data.WorkingDirectory == profile.WorkingDirectory);
            Assert.True(data.LaunchBrowser == profile.LaunchBrowser);
            Assert.True(data.LaunchUrl == profile.LaunchUrl);
            Assert.True(DictionaryEqualityComparer<string, string>.Instance.Equals(data.EnvironmentVariables.ToImmutableDictionary(), profile.EnvironmentVariables));
            Assert.True(DictionaryEqualityComparer<string, string>.Instance.Equals(data.EnvironmentVariables.ToImmutableDictionary(), profile.EnvironmentVariables));
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
            JObject jsonObject = JObject.Parse(JsonString1);

            var profiles = LaunchProfileData.DeserializeProfiles((JObject)jsonObject["profiles"]);
            Assert.Equal(4, profiles.Count);
            var profile = profiles["IIS Express"];
            Assert.Equal("IISExpress", profile.CommandName);
            Assert.Equal("http://localhost:1234:/test.html", profile.LaunchUrl);
            Assert.Equal(true, profile.LaunchBrowser);
            Assert.False(profile.InMemoryProfile);

            profile = profiles["HasCustomValues"];
            Assert.Equal("c:\\test\\project", profile.WorkingDirectory);
            Assert.Equal("c:\\test\\project\\bin\\project.exe", profile.ExecutablePath);
            Assert.Equal(null, profile.LaunchBrowser);
            Assert.Equal(null, profile.LaunchUrl);
            Assert.Equal(null, profile.EnvironmentVariables);
            Assert.Equal(true, profile.OtherSettings["custom1"]);
            Assert.Equal(124, profile.OtherSettings["custom2"]);
            Assert.Equal("mycustomVal", profile.OtherSettings["custom3"]);
            Assert.False(profile.InMemoryProfile);

            profile = profiles["Docker"];
            Assert.Equal("Docker", profile.CommandName);
            Assert.Equal(null, profile.WorkingDirectory);
            Assert.Equal(null, profile.ExecutablePath);
            Assert.Equal(false, profile.LaunchBrowser);
            Assert.Equal(null, profile.LaunchUrl);
            Assert.Equal(null, profile.EnvironmentVariables);
            Assert.Equal("some option in docker", profile.OtherSettings["dockerOption1"]);
            Assert.Equal("Another option in docker", profile.OtherSettings["dockerOption2"]);
            Assert.False(profile.InMemoryProfile);

            profile = profiles["web"];
            Assert.Equal("Project", profile.CommandName);
            Assert.Equal(null, profile.WorkingDirectory);
            Assert.Equal(null, profile.ExecutablePath);
            Assert.Equal(true, profile.LaunchBrowser);
            Assert.Equal(null, profile.LaunchUrl);
            Assert.Equal("Development", profile.EnvironmentVariables["ASPNET_ENVIRONMENT"]);
            Assert.Equal("c:\\Users\\billhie\\Documents\\projects\\WebApplication8\\src\\WebApplication8", profile.EnvironmentVariables["ASPNET_APPLICATIONBASE"]);
            Assert.Equal(null, profile.OtherSettings);
            Assert.False(profile.InMemoryProfile);
        }

        [Fact]
        public void LaunchProfileData_DeserializeEmptyProfilesTests()
        {
            JObject jsonObject = JObject.Parse(JsonString2);
            var profiles = LaunchProfileData.DeserializeProfiles((JObject)jsonObject["profiles"]);
            Assert.Empty(profiles);
        }

        [Fact]
        public void LaunchProfileData_ToSerializableFormTests()
        {
            JObject jsonObject = JObject.Parse(JsonString1);
            var profiles = LaunchProfileData.DeserializeProfiles((JObject)jsonObject["profiles"]);

            var profile = profiles["IIS Express"];
            var serializableProfile = LaunchProfileData.ToSerializableForm(new LaunchProfile(profile));

            AssertEx.CollectionLength(serializableProfile, 3);
            Assert.Equal("IISExpress", serializableProfile["commandName"]);
            Assert.Equal("http://localhost:1234:/test.html", serializableProfile["launchUrl"]);
            Assert.Equal(true, serializableProfile["launchBrowser"]);

            profile = profiles["HasCustomValues"];
            serializableProfile = LaunchProfileData.ToSerializableForm(new LaunchProfile(profile));
            Assert.Equal(6, serializableProfile.Count);
            Assert.Equal("c:\\test\\project", serializableProfile["workingDirectory"]);
            Assert.Equal("c:\\test\\project\\bin\\project.exe", serializableProfile["executablePath"]);
            Assert.Equal("--arg1 --arg2", serializableProfile["commandLineArgs"]);
            Assert.Equal(true, serializableProfile["custom1"]);
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
            Assert.Equal(true, serializableProfile["launchBrowser"]);
            Assert.Equal("Development", ((IDictionary)serializableProfile["environmentVariables"])["ASPNET_ENVIRONMENT"]);
            Assert.Equal("c:\\Users\\billhie\\Documents\\projects\\WebApplication8\\src\\WebApplication8", ((IDictionary)serializableProfile["environmentVariables"])["ASPNET_APPLICATIONBASE"]);
        }

        // Json string data
string JsonString1 = @"{
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

string JsonString2 = @"{
  ""profiles"": {
  }
}";
    }
}
