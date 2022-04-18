// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
            Assert.Equal(data.EnvironmentVariables.Select(pair => (pair.Key, pair.Value)), profile.EnvironmentVariables);
            Assert.Equal(data.OtherSettings.Select(pair => (pair.Key, pair.Value)), profile.OtherSettings);
            Assert.Equal(data.InMemoryProfile, profile.DoNotPersist);
        }

        [Fact]
        public void Clone_DoesNotCopyIfAlreadyConcreteType()
        {
            var source = new LaunchProfile("Name", "Command");

            var clone = LaunchProfile.Clone(source);

            Assert.Same(clone, source);
        }

        [Fact]
        public void Clone_CopiesIfUnknownType()
        {
            var mock = new Mock<ILaunchProfile>();
            
            var clone = LaunchProfile.Clone(mock.Object);

            Assert.NotSame(clone, mock.Object);
        }

        [Fact]
        public void LaunchProfile_IsSameProfileNameTests()
        {
            Assert.True(LaunchProfile.IsSameProfileName("test", "test"));
            Assert.False(LaunchProfile.IsSameProfileName("test", "Test"));
        }
    }
}
