// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchSettingsTests
    {
        [Fact]
        public void LaunchSettings_CtorTests()
        {
            var profiles = new List<LaunchProfile>()
            {
                new LaunchProfile(){Name="abc", CommandLineArgs="test"},
                new LaunchProfile(){Name="def"},
                new LaunchProfile(){Name="ghi"},
                new LaunchProfile(){Name="foo"},
            };
            var globals = new Dictionary<string, object>()
            {
                {"var1", true },
                {"var2", "some string" }
            };

            var settings = new LaunchSettings(profiles, null, null);
            Assert.True(settings.ActiveProfile!.Name == "abc");
            Assert.Equal(profiles.Count, settings.Profiles.Count);
            Assert.Empty(settings.GlobalSettings);

            settings = new LaunchSettings(profiles, null, "ghi");
            Assert.True(settings.ActiveProfile!.Name == "ghi");

            // Test 
            settings = new LaunchSettings(profiles, globals, "foo");
            Assert.Equal(globals.Count, settings.GlobalSettings.Count);
        }
    }
}
