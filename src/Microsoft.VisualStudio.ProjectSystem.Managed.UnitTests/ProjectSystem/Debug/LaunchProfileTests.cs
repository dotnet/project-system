// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    [ProjectSystemTrait]
    public class LaunchProfileTests
    {
        [Fact]
        public void LaunchProfile_CtorTests()
        {
            LaunchProfileData data =  new LaunchProfileData()
            {
                Name = "Test", 
                ExecutablePath ="c:\\this\\is\\a\\exe\\path",
                CommandName = null,
                CommandLineArgs="args",
                WorkingDirectory="c:\\wprking\\directory\\",
                LaunchBrowser = true,
                LaunchUrl ="LaunchPage.html",
                EnvironmentVariables = new Dictionary<string, string>(){{"var1", "Value1"}, {"var2", "Value2"}},
                OtherSettings = new Dictionary<string, object>(StringComparer.Ordinal) { {"setting1", true}, { "setting2", "mysetting" } }
            };

           LaunchProfile profile = new LaunchProfile(data);
           Assert.True(data.Name == profile.Name);
           Assert.True(data.ExecutablePath == profile.ExecutablePath);
           Assert.True(data.CommandLineArgs == profile.CommandLineArgs); 
           Assert.True(data.WorkingDirectory == profile.WorkingDirectory);
           Assert.True(data.LaunchBrowser == profile.LaunchBrowser);
           Assert.True(data.LaunchUrl == profile.LaunchUrl);
           Assert.True(DictionaryEqualityComparer<string, string>.Instance.Equals(data.EnvironmentVariables.ToImmutableDictionary(), profile.EnvironmentVariables));
           Assert.True(DictionaryEqualityComparer<string, object>.Instance.Equals(data.OtherSettings.ToImmutableDictionary(), profile.OtherSettings));
           
           // Test overload
           LaunchProfile profile2 = new LaunchProfile(profile);
           Assert.True(profile2.Name == profile.Name);
           Assert.True(profile2.ExecutablePath == profile.ExecutablePath);
           Assert.True(profile2.CommandLineArgs == profile.CommandLineArgs); 
           Assert.True(profile2.WorkingDirectory == profile.WorkingDirectory);
           Assert.True(profile2.LaunchBrowser == profile.LaunchBrowser);
           Assert.True(profile2.LaunchUrl == profile.LaunchUrl);
           Assert.True(DictionaryEqualityComparer<string, string>.Instance.Equals(profile.EnvironmentVariables, profile2.EnvironmentVariables));
           Assert.True(DictionaryEqualityComparer<string, object>.Instance.Equals(profile.OtherSettings.ToImmutableDictionary(), profile2.OtherSettings));
        }

        [Fact]
        public void LaunchProfile_IsSameProfileNameTests()
        {
            Assert.True(LaunchProfile.IsSameProfileName("test", "test"));
            Assert.False(LaunchProfile.IsSameProfileName("test", "Test"));
        }
    }
}
