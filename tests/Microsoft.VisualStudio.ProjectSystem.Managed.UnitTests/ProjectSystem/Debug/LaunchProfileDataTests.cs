// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchProfileDataTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LaunchProfileData_FromILaunchProfileTests(bool isInMemory)
        {
            var profile = new LaunchProfile(
                name: "Test",
                commandName: "Test",
                executablePath: "c:\\this\\is\\a\\exe\\path",
                commandLineArgs: "args",
                workingDirectory: "c:\\working\\directory\\",
                launchBrowser: true,
                launchUrl: "LaunchPage.html",
                environmentVariables: ImmutableArray.Create(("var1", "Value1"), ("var2", "Value2")),
                otherSettings: ImmutableArray.Create(("setting1", (object)true), ("setting2", "mysetting")),
                doNotPersist: isInMemory);

            var data = LaunchProfileData.FromILaunchProfile(profile);

            Assert.Equal(data.Name, profile.Name);
            Assert.Equal(data.ExecutablePath, profile.ExecutablePath);
            Assert.Equal(data.CommandLineArgs, profile.CommandLineArgs);
            Assert.Equal(data.WorkingDirectory, profile.WorkingDirectory);
            Assert.Equal(data.LaunchBrowser, profile.LaunchBrowser);
            Assert.Equal(data.LaunchUrl, profile.LaunchUrl);
            Assert.NotNull(data.EnvironmentVariables);
            Assert.Equal(data.EnvironmentVariables.Select(pair => (pair.Key, pair.Value)), profile.EnvironmentVariables);
            Assert.NotNull(data.OtherSettings);
            Assert.Equal(data.OtherSettings.Select(pair => (pair.Key, pair.Value)), profile.OtherSettings);
            Assert.Equal(isInMemory, data.InMemoryProfile);
        }
    }
}
