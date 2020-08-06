// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Collections;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchProfileTests
    {
        [Fact]
        public void LaunchProfile_CtorTests()
        {
            var data = new LaunchProfileData()
            {
                Name = "Test",
                ExecutablePath = "c:\\this\\is\\a\\exe\\path",
                CommandName = null,
                CommandLineArgs = "args",
                WorkingDirectory = "c:\\working\\directory\\",
                LaunchBrowser = true,
                LaunchUrl = "LaunchPage.html",
                EnvironmentVariables = new Dictionary<string, string>() { { "var1", "Value1" }, { "var2", "Value2" } },
                OtherSettings = new Dictionary<string, object>(StringComparer.Ordinal) { { "setting1", true }, { "setting2", "mysetting" } },
                InMemoryProfile = true
            };

            var profile = new LaunchProfile(data);
            Assert.Equal(data.Name, profile.Name);
            Assert.Equal(data.ExecutablePath, profile.ExecutablePath);
            Assert.Equal(data.CommandLineArgs, profile.CommandLineArgs);
            Assert.Equal(data.WorkingDirectory, profile.WorkingDirectory);
            Assert.Equal(data.LaunchBrowser, profile.LaunchBrowser);
            Assert.Equal(data.LaunchUrl, profile.LaunchUrl);
            Assert.True(DictionaryEqualityComparer<string, string>.Instance.Equals(data.EnvironmentVariables.ToImmutableDictionary(), profile.EnvironmentVariables!));
            Assert.True(DictionaryEqualityComparer<string, object>.Instance.Equals(data.OtherSettings.ToImmutableDictionary(), profile.OtherSettings!));
            Assert.Equal(data.InMemoryProfile, profile.DoNotPersist);

            // Test overload
            var profile2 = new LaunchProfile(profile);
            Assert.Equal(profile.Name, profile2.Name);
            Assert.Equal(profile.ExecutablePath, profile2.ExecutablePath);
            Assert.Equal(profile.CommandLineArgs, profile2.CommandLineArgs);
            Assert.Equal(profile.WorkingDirectory, profile2.WorkingDirectory);
            Assert.Equal(profile.LaunchBrowser, profile2.LaunchBrowser);
            Assert.Equal(profile.LaunchUrl, profile2.LaunchUrl);
            Assert.True(DictionaryEqualityComparer<string, string>.Instance.Equals(profile.EnvironmentVariables!, profile2.EnvironmentVariables!));
            Assert.True(DictionaryEqualityComparer<string, object>.Instance.Equals(profile.OtherSettings!.ToImmutableDictionary(), profile2.OtherSettings!));
            Assert.Equal(profile.DoNotPersist, profile2.DoNotPersist);
        }

        [Fact]
        public void LaunchProfile_IsSameProfileNameTests()
        {
            Assert.True(LaunchProfile.IsSameProfileName("test", "test"));
            Assert.False(LaunchProfile.IsSameProfileName("test", "Test"));
        }
    }
}
