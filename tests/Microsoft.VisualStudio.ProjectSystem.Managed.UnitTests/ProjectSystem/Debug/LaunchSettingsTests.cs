// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchSettingsTests
    {
        [Fact]
        public void LaunchSettings_CtorTests()
        {
            var profiles = new List<LaunchProfile>()
            {
                new LaunchProfile("abc", null, commandLineArgs: "test"),
                new LaunchProfile("def", null),
                new LaunchProfile("ghi", null),
                new LaunchProfile("foo", null),
            };

            var globals = ImmutableDictionary<string, object>.Empty
                .Add("var1", true)
                .Add("var2", "some string");

            var settings = new LaunchSettings(profiles);
            Assert.NotNull(settings.ActiveProfile);
            Assert.True(settings.ActiveProfile.Name == "abc");
            Assert.Equal(profiles.Count, settings.Profiles.Count);
            Assert.Empty(settings.GlobalSettings);

            settings = new LaunchSettings(profiles, activeProfileName: "ghi");
            Assert.NotNull(settings.ActiveProfile);
            Assert.True(settings.ActiveProfile.Name == "ghi");

            // Test 
            settings = new LaunchSettings(profiles, globals, activeProfileName: "foo");
            Assert.Equal(globals.Count, settings.GlobalSettings.Count);
        }
    }
}
