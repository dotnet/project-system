// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    public class DebugProfileEnumValuesGeneratorTests
    {
        private readonly List<ILaunchProfile> _profiles = new()
        {
            new LaunchProfile("Profile1", null, launchBrowser: true),
            new LaunchProfile("MyCommand", null),
            new LaunchProfile("Foo", null),
            new LaunchProfile("Bar", null),
            new LaunchProfile("Foo & Bar", null)
        };

        [Fact]
        public async Task DebugProfileEnumValuesGenerator_GetListsValuesAsyncTests()
        {
            var testProfiles = new Mock<ILaunchSettings>();
            testProfiles.Setup(m => m.ActiveProfile).Returns(() => { return _profiles[1]; });
            testProfiles.Setup(m => m.Profiles).Returns(() =>
            {
                return _profiles.ToImmutableList();
            });

            var moqProfileProvider = new Mock<ILaunchSettingsProvider>();
            moqProfileProvider.Setup(p => p.CurrentSnapshot).Returns(testProfiles.Object);
            var threadingService = IProjectThreadingServiceFactory.Create();

            var generator =
                new DebugProfileEnumValuesGenerator(moqProfileProvider.Object, threadingService);
            ICollection<IEnumValue> results = await generator.GetListedValuesAsync();
            AssertEx.CollectionLength(results, 5);
            Assert.True(results.ElementAt(0).Name == "Profile1" && results.ElementAt(0).DisplayName == "Profile1");
            Assert.True(results.ElementAt(1).Name == "MyCommand" && results.ElementAt(1).DisplayName == "MyCommand");
            Assert.True(results.ElementAt(2).Name == "Foo" && results.ElementAt(2).DisplayName == "Foo");
            Assert.True(results.ElementAt(3).Name == "Bar" && results.ElementAt(3).DisplayName == "Bar");
            Assert.True(results.ElementAt(4).Name == "Foo & Bar" && results.ElementAt(4).DisplayName == "Foo && Bar");
        }

        [Fact]
        public async Task DebugProfileEnumValuesGenerator_TryCreateEnumValueAsyncTests()
        {
            var testProfiles = new Mock<ILaunchSettings>();
            testProfiles.Setup(m => m.ActiveProfile).Returns(() => { return _profiles[1]; });
            testProfiles.Setup(m => m.Profiles).Returns(() =>
            {
                return _profiles.ToImmutableList();
            });

            var moqProfileProvider = new Mock<ILaunchSettingsProvider>();
            moqProfileProvider.Setup(p => p.CurrentSnapshot).Returns(testProfiles.Object);
            var threadingService = IProjectThreadingServiceFactory.Create();

            var generator =
                new DebugProfileEnumValuesGenerator(moqProfileProvider.Object, threadingService);

            Assert.False(generator.AllowCustomValues);
            IEnumValue? result = await generator.TryCreateEnumValueAsync("Profile1");
            Assert.True(result!.Name == "Profile1" && result.DisplayName == "Profile1");
            result = await generator.TryCreateEnumValueAsync("MyCommand");
            Assert.True(result!.Name == "MyCommand" && result.DisplayName == "MyCommand");

            // case sensitive check
            result = await generator.TryCreateEnumValueAsync("mycommand");
            Assert.Null(result);

            result = await generator.TryCreateEnumValueAsync("Foo");
            Assert.True(result!.Name == "Foo" && result.DisplayName == "Foo");
            result = await generator.TryCreateEnumValueAsync("Bar");
            Assert.True(result!.Name == "Bar" && result.DisplayName == "Bar");
        }
    }
}
