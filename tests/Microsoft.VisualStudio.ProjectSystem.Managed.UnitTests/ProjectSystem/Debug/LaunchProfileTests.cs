// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchProfileTests
    {
        [Fact]
        public void LaunchProfile_CtorTests()
        {
            string? name = "Test";
            string? executablePath = "c:\\this\\is\\a\\exe\\path";
            string? commandName = null;
            string? commandLineArgs = "args";
            string? workingDirectory = "c:\\working\\directory\\";
            bool launchBrowser = true;
            string? launchUrl = "LaunchPage.html";
            var environmentVariables = ImmutableArray.Create(("var1", "Value1"), ("var2", "Value2"));
            var otherSettings = ImmutableArray.Create<(string, object)>(("setting1", true), ("setting2", "mysetting"));
            var doNotPersist = true;

            var profile = new LaunchProfile(
                name: name,
                executablePath: executablePath,
                commandName: commandName,
                commandLineArgs: commandLineArgs,
                workingDirectory: workingDirectory,
                launchBrowser: launchBrowser,
                launchUrl: launchUrl,
                environmentVariables: environmentVariables,
                otherSettings: otherSettings,
                doNotPersist: doNotPersist);

            Assert.Equal(name, profile.Name);
            Assert.Equal(executablePath, profile.ExecutablePath);
            Assert.Equal(commandLineArgs, profile.CommandLineArgs);
            Assert.Equal(workingDirectory, profile.WorkingDirectory);
            Assert.Equal(launchBrowser, profile.LaunchBrowser);
            Assert.Equal(launchUrl, profile.LaunchUrl);
            Assert.Equal(environmentVariables, profile.EnvironmentVariables);
            Assert.Equal(otherSettings, profile.OtherSettings);
            Assert.Equal(doNotPersist, profile.DoNotPersist);
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
