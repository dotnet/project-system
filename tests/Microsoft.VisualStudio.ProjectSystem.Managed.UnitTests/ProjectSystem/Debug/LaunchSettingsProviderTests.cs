// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable CS0618 // Type or member is obsolete

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders;
using Microsoft.VisualStudio.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchSettingsProviderTests
    {
        internal static LaunchSettingsUnderTest GetLaunchSettingsProvider(IFileSystem? fileSystem, string? appDesignerFolder = @"c:\test\Project1\Properties", string activeProfile = "")
        {
            var activeProfileValue = new Mock<IEnumValue>();
            activeProfileValue.Setup(s => s.Name).Returns(activeProfile);
            var debuggerData = new PropertyPageData(ProjectDebugger.SchemaName, ProjectDebugger.ActiveDebugProfileProperty, activeProfileValue.Object);

            var specialFilesManager = IActiveConfiguredValueFactory.ImplementValue<IAppDesignerFolderSpecialFileProvider?>(() => IAppDesignerFolderSpecialFileProviderFactory.ImplementGetFile(appDesignerFolder));
            var project = UnconfiguredProjectFactory.Create(fullPath: @"c:\test\Project1\Project1.csproj");
            var properties = ProjectPropertiesFactory.Create(project, new[] { debuggerData });
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project, IProjectThreadingServiceFactory.Create(), null, properties);
            var projectServices = IUnconfiguredProjectServicesFactory.Create(IProjectAsynchronousTasksServiceFactory.Create());
            var projectFaultHandlerService = IProjectFaultHandlerServiceFactory.Create();
            var provider = new LaunchSettingsUnderTest(project, projectServices, fileSystem ?? new IFileSystemMock(), commonServices, null, specialFilesManager, projectFaultHandlerService, new DefaultLaunchProfileProvider(project));
            return provider;
        }

        internal static void SetJsonSerializationProviders(LaunchSettingsUnderTest provider)
        {
            var mockIJsonSection = new Mock<IJsonSection>();
            mockIJsonSection.Setup(s => s.JsonSection).Returns("iisSettings");
            mockIJsonSection.Setup(s => s.SerializationType).Returns(typeof(IISSettingsData));
            var lazyProvider = new Lazy<ILaunchSettingsSerializationProvider, IJsonSection>(() =>
            {
                var mockSerializer = new Mock<ILaunchSettingsSerializationProvider>();
                return mockSerializer.Object;
            }, mockIJsonSection.Object, true);
            var settingsProviders = new OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject?)null)
            {
                new Lazy<ILaunchSettingsSerializationProvider, IJsonSection>(() => lazyProvider.Value, mockIJsonSection.Object)
            };
            provider.SetSettingsProviderCollection(settingsProviders);
        }

        [Fact]
        public void WhenNoAppDesignerFolder_LaunchSettingsIsInRoot()
        {
            using var provider = GetLaunchSettingsProvider(null, appDesignerFolder: null);
            Assert.Equal(@"c:\test\Project1\launchSettings.json", provider.LaunchSettingsFile);
        }

        [Theory]
        [InlineData(@"C:\Properties", @"C:\Properties\launchSettings.json")]
        [InlineData(@"C:\Project\Properties", @"C:\Project\Properties\launchSettings.json")]
        [InlineData(@"C:\Project\My Project", @"C:\Project\My Project\launchSettings.json")]
        public async Task WhenAppDesignerFolder_LaunchSettingsIsInAppDesignerFolder(string appDesignerFolder, string expected)
        {
            using var provider = GetLaunchSettingsProvider(null, appDesignerFolder: appDesignerFolder);
            var result = await provider.GetLaunchSettingsFilePathNoCacheAsync();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ActiveProfileTests()
        {
            string activeProfile = "MyCommand";
            var testProfiles = new Mock<ILaunchSettings>();
            testProfiles.Setup(m => m.ActiveProfile).Returns(new LaunchProfile() { Name = activeProfile });

            using var provider = GetLaunchSettingsProvider(null);
            Assert.Null(provider.ActiveProfile);

            provider.SetCurrentSnapshot(testProfiles.Object);
            Assert.NotNull(provider.ActiveProfile);
            Assert.Equal(activeProfile, provider.ActiveProfile?.Name);
        }

        [Fact]
        public async Task UpdateProfiles_NoSettingsFile()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            await provider.UpdateProfilesAsyncTest(null);
            Assert.Single(provider.CurrentSnapshot.Profiles);
            Assert.Equal("Project", provider.CurrentSnapshot.ActiveProfile!.CommandName);
        }

        [Fact]
        public async Task UpdateProfilesBasicSettingsFile()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonString1);

            await provider.UpdateProfilesAsyncTest(null);
            Assert.Equal(4, provider.CurrentSnapshot.Profiles.Count);
            Assert.Empty(provider.CurrentSnapshot.GlobalSettings);
            Assert.Equal("IIS Express", provider.CurrentSnapshot.ActiveProfile!.Name);
        }

        [Fact]
        public async Task UpdateProfilesSetActiveProfileFromProperty()
        {
            var moqFS = new IFileSystemMock();
            using var provider1 = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider1.LaunchSettingsFile, JsonString1);

            // Change the value of activeDebugProfile to web it should be the active one. Simulates a change
            // on disk doesn't affect active profile
            using var provider2 = GetLaunchSettingsProvider(moqFS, activeProfile: "web");
            await provider2.UpdateProfilesAsyncTest(null);
            Assert.Equal("web", provider2.CurrentSnapshot.ActiveProfile!.Name);
        }

        [Fact]
        public async Task UpdateProfiles_ChangeActiveProfileOnly()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonString1);
            await provider.UpdateProfilesAsyncTest(null);
            provider.SetNextVersionTest(123);

            // don't change file on disk, just active one
            await provider.UpdateProfilesAsyncTest("Docker");
            Assert.Equal(4, provider.CurrentSnapshot.Profiles.Count);
            Assert.Empty(provider.CurrentSnapshot.GlobalSettings);
            Assert.Equal("Docker", provider.CurrentSnapshot.ActiveProfile!.Name);
            Assert.Equal(123, ((IVersionedLaunchSettings)provider.CurrentSnapshot).Version);
        }

        [Fact]
        public async Task UpdateProfiles_BadJsonShouldLeaveProfilesStable()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonString1);
            await provider.UpdateProfilesAsyncTest(null);

            moqFS.WriteAllText(provider.LaunchSettingsFile, BadJsonString);
            await provider.UpdateProfilesAsyncTest("Docker");
            Assert.Equal(4, provider.CurrentSnapshot.Profiles.Count);
            Assert.Empty(provider.CurrentSnapshot.GlobalSettings);
            Assert.Equal("IIS Express", provider.CurrentSnapshot.ActiveProfile!.Name);
        }

        [Fact]
        public async Task UpdateProfiles_SetsErrorProfileTests()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, BadJsonString);

            await provider.UpdateProfilesAsyncTest("Docker");
            Assert.Single(provider.CurrentSnapshot.Profiles);
            Assert.Equal(LaunchSettingsProvider.ErrorProfileCommandName, provider.CurrentSnapshot.ActiveProfile!.CommandName);
            Assert.True(((IPersistOption)provider.CurrentSnapshot.ActiveProfile).DoNotPersist);
        }

        [Fact]
        public async Task UpdateProfiles_MergeInMemoryProfiles()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonString1);

            var curProfiles = new Mock<ILaunchSettings>();
            curProfiles.Setup(m => m.Profiles).Returns(() =>
            {
                return new List<ILaunchProfile>()
                {
                        { new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true, DoNotPersist = true } },
                        { new LaunchProfile() { Name = "InMemory1", DoNotPersist = true} },
                        { new LaunchProfile() { Name = "ShouldNotBeIncluded", CommandName=LaunchSettingsProvider.ErrorProfileCommandName, DoNotPersist = true} }
                }.ToImmutableList();
            });

            provider.SetCurrentSnapshot(curProfiles.Object);

            await provider.UpdateProfilesAsyncTest(null);
            Assert.Equal(5, provider.CurrentSnapshot.Profiles.Count);
            Assert.Equal("InMemory1", provider.CurrentSnapshot.Profiles[1].Name);
            Assert.True(provider.CurrentSnapshot.Profiles[1].IsInMemoryObject());
            Assert.False(provider.CurrentSnapshot.Profiles[0].IsInMemoryObject());
        }

        [Fact]
        public async Task UpdateProfiles_MergeInMemoryProfiles_AddProfileAtEnd()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonString1);

            var curProfiles = new Mock<ILaunchSettings>();
            curProfiles.Setup(m => m.Profiles).Returns(() =>
            {
                return new List<ILaunchProfile>()
                {
                        { new LaunchProfile() { Name = "profile1", CommandName="IISExpress", LaunchBrowser=true} },
                        { new LaunchProfile() { Name ="profile2", CommandName="IISExpress", LaunchBrowser=true} },
                        { new LaunchProfile() { Name ="profile3", CommandName="IISExpress", LaunchBrowser=true} },
                        { new LaunchProfile() { Name ="profile4", CommandName="IISExpress", LaunchBrowser=true} },
                        { new LaunchProfile() { Name ="profile5", CommandName="IISExpress", LaunchBrowser=true} },
                        { new LaunchProfile() { Name = "InMemory1", DoNotPersist = true} }
                }.ToImmutableList();
            });

            provider.SetCurrentSnapshot(curProfiles.Object);

            await provider.UpdateProfilesAsyncTest(null);
            Assert.Equal(5, provider.CurrentSnapshot.Profiles.Count);
            Assert.Equal("InMemory1", provider.CurrentSnapshot.Profiles[provider.CurrentSnapshot.Profiles.Count - 1].Name);
            Assert.True(provider.CurrentSnapshot.Profiles[provider.CurrentSnapshot.Profiles.Count - 1].IsInMemoryObject());
        }

        [Fact]
        public async Task UpdateProfiles_MergeInMemoryGlobalSettings()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonStringWithWebSettings);

            var curProfiles = new Mock<ILaunchSettings>();
            curProfiles.Setup(m => m.Profiles).Returns(() =>
            {
                return new List<ILaunchProfile>()
                {
                        { new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true, DoNotPersist = true } },
                        { new LaunchProfile() { Name = "InMemory1", DoNotPersist = true} }
                }.ToImmutableList();
            });
            curProfiles.Setup(m => m.GlobalSettings).Returns(() =>
            {
                return new Dictionary<string, object>()
                {
                        { "iisSettings", new IISSettingsData() {   AnonymousAuthentication=true, DoNotPersist = true } },
                        { "SomeSettings", new IISSettingsData() {  AnonymousAuthentication = false,  DoNotPersist = false } },
                        { "InMemoryUnique", new IISSettingsData() {  AnonymousAuthentication = false,  DoNotPersist = true } },
                }.ToImmutableDictionary();
            });

            provider.SetCurrentSnapshot(curProfiles.Object);

            await provider.UpdateProfilesAsyncTest(null);
            Assert.Equal(2, provider.CurrentSnapshot.GlobalSettings.Count);
            Assert.False(provider.CurrentSnapshot.GlobalSettings["iisSettings"].IsInMemoryObject());
            Assert.True(provider.CurrentSnapshot.GlobalSettings["InMemoryUnique"].IsInMemoryObject());
        }

        [Fact]
        public async Task SettingsFileHasChangedTests()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            Assert.True(await provider.SettingsFileHasChangedAsyncTest());
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonString1);

            Assert.True(await provider.SettingsFileHasChangedAsyncTest());
            provider.LastSettingsFileSyncTimeTest = moqFS.GetLastFileWriteTimeOrMinValueUtc(provider.LaunchSettingsFile);
            Assert.False(await provider.SettingsFileHasChangedAsyncTest());
        }

        [Fact]
        public async Task ReadProfilesFromDisk_NoFile()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
{
    return provider.ReadSettingsFileFromDiskTestAsync();
});
        }

        [Fact]
        public async Task ReadProfilesFromDisk_GoodFile()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonString1);

            var launchSettings = await provider.ReadSettingsFileFromDiskTestAsync();

            Assert.Equal(4, launchSettings.Profiles!.Count);
        }

        [Fact]
        public async Task ReadProfilesFromDisk_BadJsonFile()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, BadJsonString);

            await Assert.ThrowsAsync<JsonReaderException>(() =>
            {
                return provider.ReadSettingsFileFromDiskTestAsync();
            });
        }

        [Fact]
        public async Task ReadProfilesFromDisk_JsonWithExtensionsNoProvider()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonStringWithWebSettings);

            var launchSettings = await provider.ReadSettingsFileFromDiskTestAsync();

            AssertEx.CollectionLength(launchSettings.Profiles!, 2);
            Assert.Single(launchSettings.OtherSettings);
            Assert.True(launchSettings.OtherSettings!["iisSettings"] is JObject);
        }

        [Fact]
        public async Task ReadProfilesFromDisk_JsonWithExtensionsWithProvider()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonStringWithWebSettings);

            // Set the serialization provider
            SetJsonSerializationProviders(provider);

            var launchSettings = await provider.ReadSettingsFileFromDiskTestAsync();

            AssertEx.CollectionLength(launchSettings.Profiles!, 2);
            Assert.Single(launchSettings.OtherSettings);
            Assert.True(launchSettings.OtherSettings!["iisSettings"] is IISSettingsData);
        }

        [Fact]
        public async Task SaveProfilesToDiskTests()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            var profiles = new List<ILaunchProfile>()
                {
                    { new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true } },
                    {new LaunchProfile() { Name = "bar", ExecutablePath="c:\\test\\project\\bin\\test.exe", CommandLineArgs=@"-someArg"} }
                };

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.ActiveProfile).Returns(() => { return profiles[0]; });
            testSettings.Setup(m => m.Profiles).Returns(() =>
            {
                return profiles.ToImmutableList();
            });
            testSettings.Setup(m => m.GlobalSettings).Returns(() =>
            {
                var iisSettings = new IISSettingsData()
                {
                    AnonymousAuthentication = false,
                    WindowsAuthentication = true,
                    IISExpressBindingData = new ServerBindingData()
                    {
                        ApplicationUrl = "http://localhost:12345/",
                        SSLPort = 44301
                    }
                };
                return ImmutableStringDictionary<object>.EmptyOrdinal.Add("iisSettings", iisSettings);
            });

            await provider.SaveSettingsToDiskAsyncTest(testSettings.Object);

            // Last Write time should be set
            Assert.Equal(moqFS.GetLastFileWriteTimeOrMinValueUtc(provider.LaunchSettingsFile), provider.LastSettingsFileSyncTimeTest);

            // Check disk contents
            Assert.Equal(JsonStringWithWebSettings, moqFS.ReadAllText(provider.LaunchSettingsFile), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task LaunchSettingsFile_Changed()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            provider.SetNextVersionTest(123);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonString1);
            // Wait for completion of task
            await provider.LaunchSettingsFile_ChangedTest();

            Assert.NotNull(provider.CurrentSnapshot);
            Assert.Equal(4, provider.CurrentSnapshot.Profiles.Count);
            Assert.Equal(123, ((IVersionedLaunchSettings)provider.CurrentSnapshot).Version);
        }

        [Fact]
        public async Task LaunchSettingsFile_TestIgnoreFlag()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            string fileName = await provider.GetLaunchSettingsFilePathNoCacheAsync();
            // Write file and generate disk change
            moqFS.WriteAllText(fileName, JsonString1);

            // Set the ignore flag. It should be ignored.
            provider.LastSettingsFileSyncTimeTest = DateTime.MinValue;
            provider.SetIgnoreFileChanges(true);
            Assert.Equal(provider.LaunchSettingsFile_ChangedTest(), Task.CompletedTask);
            Assert.Null(provider.CurrentSnapshot);

            // Should run this time
            provider.SetIgnoreFileChanges(false);
            await provider.LaunchSettingsFile_ChangedTest();
            Assert.NotNull(provider.CurrentSnapshot);
            Assert.Equal(4, provider.CurrentSnapshot.Profiles.Count);
        }

        [Fact]
        public async Task LaunchSettingsFile_TestTimeStampFlag()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonString1);
            await provider.LaunchSettingsFile_ChangedTest();
            Assert.Equal(4, provider.CurrentSnapshot.Profiles.Count);

            // Write new file, but set the timestamp to match
            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonStringWithWebSettings);
            provider.LastSettingsFileSyncTimeTest = moqFS.GetLastFileWriteTimeOrMinValueUtc(provider.LaunchSettingsFile);
            Assert.Equal(provider.LaunchSettingsFile_ChangedTest(), Task.CompletedTask);
            AssertEx.CollectionLength(provider.CurrentSnapshot.Profiles, 4);

            moqFS.WriteAllText(provider.LaunchSettingsFile, JsonStringWithWebSettings);
            await provider.LaunchSettingsFile_ChangedTest();
            AssertEx.CollectionLength(provider.CurrentSnapshot.Profiles, 2);
        }

        [Fact]
        public async Task Dispose_WhenNotActivated_DoesNotThrow()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);

            await provider.DisposeAsync();

            Assert.True(provider.IsDisposed);
        }

        [Fact]
        public async Task UpdateAndSaveProfilesAsync()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            var profiles = new List<ILaunchProfile>()
                {
                    {new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true } },
                    {new LaunchProfile() { Name = "bar", ExecutablePath ="c:\\test\\project\\bin\\test.exe", CommandLineArgs=@"-someArg"} }
                };

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.ActiveProfile).Returns(() => { return profiles[0]; });
            testSettings.Setup(m => m.Profiles).Returns(() =>
            {
                return profiles.ToImmutableList();
            });

            testSettings.Setup(m => m.GlobalSettings).Returns(() =>
            {
                var iisSettings = new IISSettingsData()
                {
                    AnonymousAuthentication = false,
                    WindowsAuthentication = true,
                    IISExpressBindingData = new ServerBindingData()
                    {
                        ApplicationUrl = "http://localhost:12345/",
                        SSLPort = 44301
                    }
                };
                return ImmutableStringDictionary<object>.EmptyOrdinal.Add("iisSettings", iisSettings);
            });

            // Setup SCC to verify it is called before modifying the file
            var mockScc = new Mock<ISourceCodeControlIntegration>(MockBehavior.Strict);
            mockScc.Setup(m => m.CanChangeProjectFilesAsync(It.IsAny<IReadOnlyCollection<string>>())).Returns(Task.FromResult(true));
            var sccProviders = new OrderPrecedenceImportCollection<ISourceCodeControlIntegration>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject?)null)
                {
                    mockScc.Object
                };
            provider.SetSourceControlProviderCollection(sccProviders);
            provider.SetNextVersionTest(123);

            await provider.UpdateAndSaveSettingsAsync(testSettings.Object);

            // Check disk contents
            Assert.Equal(JsonStringWithWebSettings, moqFS.ReadAllText(provider.LaunchSettingsFile), ignoreLineEndingDifferences: true);

            // Check snapshot
            AssertEx.CollectionLength(provider.CurrentSnapshot.Profiles, 2);
            Assert.Single(provider.CurrentSnapshot.GlobalSettings);
            Assert.Equal(123, ((IVersionedLaunchSettings)provider.CurrentSnapshot).Version);

            // Verify the activeProfile is set to the first one since no existing snapshot
            Assert.Equal("IIS Express", provider.CurrentSnapshot.ActiveProfile!.Name);

            mockScc.Verify();
        }

        [Fact]
        public async Task UpdateAndSaveProfilesAsync_ActiveProfilePreserved()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS, "Properties", "bar");
            var existingSettings = new Mock<ILaunchSettings>();
            existingSettings.Setup(m => m.ActiveProfile).Returns(new LaunchProfile() { Name = "bar" });
            provider.SetCurrentSnapshot(existingSettings.Object);
            var profiles = new List<ILaunchProfile>()
                {
                    {new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true } },
                    {new LaunchProfile() { Name = "bar", ExecutablePath ="c:\\test\\project\\bin\\test.exe", CommandLineArgs=@"-someArg"} }
                };

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.ActiveProfile).Returns(() => { return profiles[0]; });
            testSettings.Setup(m => m.Profiles).Returns(() =>
            {
                return profiles.ToImmutableList();
            });

            testSettings.Setup(m => m.GlobalSettings).Returns(() => ImmutableStringDictionary<object>.EmptyOrdinal);

            await provider.UpdateAndSaveSettingsAsync(testSettings.Object);

            // Verify the activeProfile hasn't changed
            Assert.Equal("bar", provider.CurrentSnapshot.ActiveProfile!.Name);
        }

        [Fact]
        public async Task UpdateAndSaveProfilesAsync_NoPersist()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS, "Properties", "bar");
            var existingSettings = new Mock<ILaunchSettings>();
            existingSettings.Setup(m => m.ActiveProfile).Returns(new LaunchProfile() { Name = "bar" });
            provider.SetCurrentSnapshot(existingSettings.Object);
            var profiles = new List<ILaunchProfile>()
                {
                    {new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true } },
                    {new LaunchProfile() { Name = "bar", ExecutablePath ="c:\\test\\project\\bin\\test.exe", CommandLineArgs=@"-someArg"} }
                };

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.ActiveProfile).Returns(() => { return profiles[0]; });
            testSettings.Setup(m => m.Profiles).Returns(() =>
            {
                return profiles.ToImmutableList();
            });

            testSettings.Setup(m => m.GlobalSettings).Returns(() => ImmutableStringDictionary<object>.EmptyOrdinal);

            var mockScc = new Mock<ISourceCodeControlIntegration>(MockBehavior.Strict);
            var sccProviders = new OrderPrecedenceImportCollection<ISourceCodeControlIntegration>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject?)null)
                {
                    mockScc.Object
                };
            provider.SetSourceControlProviderCollection(sccProviders);

            await provider.UpdateAndSaveSettingsInternalAsyncTest(testSettings.Object, persistToDisk: false);

            // Verifify the settings haven't been persisted and the sccProvider wasn't called to checkout the file
            Assert.False(moqFS.FileExists(provider.LaunchSettingsFile));
            mockScc.Verify();
        }

        [Theory]
        [InlineData(true, 0, false)]
        [InlineData(false, 2, false)]
        [InlineData(false, 2, true)]
        public async Task AddOrUpdateProfileAsync_ProfileDoesntExist(bool addToFront, int expectedIndex, bool isInMemory)
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            var profiles = new List<ILaunchProfile>()
                {
                    {new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true } },
                    {new LaunchProfile() { Name = "bar", ExecutablePath ="c:\\test\\project\\bin\\test.exe", CommandLineArgs=@"-someArg"} }
                };

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.Profiles).Returns(profiles.ToImmutableList());

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            var newProfile = new LaunchProfile() { Name = "test", CommandName = "Test", DoNotPersist = isInMemory };

            await provider.AddOrUpdateProfileAsync(newProfile, addToFront);

            // Check disk file was written unless not in memory
            Assert.Equal(!isInMemory, moqFS.FileExists(provider.LaunchSettingsFile));

            // Check snapshot
            AssertEx.CollectionLength(provider.CurrentSnapshot.Profiles, 3);
            Assert.Equal("Test", provider.CurrentSnapshot.Profiles[expectedIndex].CommandName);
            Assert.Null(provider.CurrentSnapshot.Profiles[expectedIndex].ExecutablePath);
            Assert.True(((IVersionedLaunchSettings)provider.CurrentSnapshot).Version >= 123);
        }

        [Theory]
        [InlineData(true, 0, false, false)]
        [InlineData(false, 1, false, false)]
        [InlineData(false, 1, true, false)]
        [InlineData(false, 1, true, true)]
        public async Task AddOrUpdateProfileAsync_ProfileExists(bool addToFront, int expectedIndex, bool isInMemory, bool existingIsInMemory)
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            var profiles = new List<ILaunchProfile>()
                {
                    {new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true } },
                    {new LaunchProfile() { Name = "test", ExecutablePath ="c:\\test\\project\\bin\\test.exe", CommandLineArgs=@"-someArg", DoNotPersist = existingIsInMemory} },
                    {new LaunchProfile() { Name = "bar", ExecutablePath ="c:\\test\\project\\bin\\bar.exe"} }
                };

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.Profiles).Returns(profiles.ToImmutableList());

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            var newProfile = new LaunchProfile() { Name = "test", CommandName = "Test", DoNotPersist = isInMemory };

            await provider.AddOrUpdateProfileAsync(newProfile, addToFront);

            // Check disk file was written unless in memory profile
            Assert.Equal(!isInMemory || (isInMemory && !existingIsInMemory), moqFS.FileExists(provider.LaunchSettingsFile));

            // Check snapshot
            AssertEx.CollectionLength(provider.CurrentSnapshot.Profiles, 3);
            Assert.Equal("test", provider.CurrentSnapshot.Profiles[expectedIndex].Name);
            Assert.Equal("Test", provider.CurrentSnapshot.Profiles[expectedIndex].CommandName);
            Assert.Null(provider.CurrentSnapshot.Profiles[expectedIndex].ExecutablePath);
            Assert.True(((IVersionedLaunchSettings)provider.CurrentSnapshot).Version >= 123);
        }

        [Theory]
        [InlineData(1, false, false)]
        [InlineData(1, true, false)]
        [InlineData(1, true, true)]
        public async Task UpdateProfileAsync_ProfileExists(int expectedIndex, bool isInMemory, bool existingIsInMemory)
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            var profiles = new List<ILaunchProfile>()
                {
                    {new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true } },
                    {new LaunchProfile() { Name = "test", ExecutablePath ="c:\\test\\project\\bin\\test.exe", CommandLineArgs=@"-someArg", DoNotPersist = existingIsInMemory} },
                    {new LaunchProfile() { Name = "bar", ExecutablePath ="c:\\test\\project\\bin\\bar.exe"} }
                };

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.Profiles).Returns(profiles.ToImmutableList());

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            var newProfile = new LaunchProfile() { Name = "test", CommandName = "Test", DoNotPersist = isInMemory };

            await provider.TryUpdateProfileAsync("test", p =>
            {
                p.CommandName = "Test";
                var persist = (IWritablePersistOption)p;
                persist.DoNotPersist = isInMemory;
            });

            // Check disk file was written unless in memory profile
            Assert.Equal(!isInMemory || (isInMemory && !existingIsInMemory), moqFS.FileExists(provider.LaunchSettingsFile));

            // Check snapshot
            AssertEx.CollectionLength(provider.CurrentSnapshot.Profiles, 3);
            Assert.Equal("test", provider.CurrentSnapshot.Profiles[expectedIndex].Name);
            Assert.Equal("Test", provider.CurrentSnapshot.Profiles[expectedIndex].CommandName);
            Assert.Equal("c:\\test\\project\\bin\\test.exe", provider.CurrentSnapshot.Profiles[expectedIndex].ExecutablePath);
            Assert.True(((IVersionedLaunchSettings)provider.CurrentSnapshot).Version >= 123);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RemoveProfileAsync_ProfileExists(bool isInMemory)
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            var profiles = new List<ILaunchProfile>()
                {
                    {new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true } },
                    {new LaunchProfile() { Name = "test", ExecutablePath ="c:\\test\\project\\bin\\test.exe", CommandLineArgs=@"-someArg", DoNotPersist = isInMemory} },
                    {new LaunchProfile() { Name = "bar", ExecutablePath ="c:\\test\\project\\bin\\bar.exe"} }
                };

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.Profiles).Returns(profiles.ToImmutableList());

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            await provider.RemoveProfileAsync("test");

            // Check disk file was written
            Assert.Equal(!isInMemory, moqFS.FileExists(provider.LaunchSettingsFile));

            // Check snapshot
            AssertEx.CollectionLength(provider.CurrentSnapshot.Profiles, 2);
            Assert.Null(provider.CurrentSnapshot.Profiles.FirstOrDefault(p => p.Name!.Equals("test")));
            Assert.True(((IVersionedLaunchSettings)provider.CurrentSnapshot).Version >= 123);
        }

        [Fact]
        public async Task RemoveProfileAsync_ProfileDoesntExists()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            var profiles = new List<ILaunchProfile>()
                {
                    {new LaunchProfile() { Name = "IIS Express", CommandName="IISExpress", LaunchBrowser=true } },
                    {new LaunchProfile() { Name = "bar", ExecutablePath ="c:\\test\\project\\bin\\bar.exe"} }
                };

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.Profiles).Returns(profiles.ToImmutableList());
            var versionedTestSettings = testSettings.As<IVersionedLaunchSettings>();
            versionedTestSettings.Setup(m => m.Version).Returns(42);

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            await provider.RemoveProfileAsync("test");

            // Check disk file was not written
            Assert.False(moqFS.FileExists(provider.LaunchSettingsFile));

            // Check snapshot
            AssertEx.CollectionLength(provider.CurrentSnapshot.Profiles, 2);
            Assert.Equal(42, ((IVersionedLaunchSettings)provider.CurrentSnapshot).Version);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AddOrUpdateGlobalSettingAsync_SettingDoesntExist(bool isInMemory)
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            SetJsonSerializationProviders(provider);

            var globalSettings = ImmutableStringDictionary<object>.EmptyOrdinal.Add("test", new LaunchProfile());

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.GlobalSettings).Returns(globalSettings);
            testSettings.Setup(m => m.Profiles).Returns(ImmutableList<ILaunchProfile>.Empty);

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            var newSettings = new IISSettingsData() { WindowsAuthentication = true, DoNotPersist = isInMemory };

            await provider.AddOrUpdateGlobalSettingAsync("iisSettings", newSettings);

            // Check disk file was written
            Assert.Equal(!isInMemory, moqFS.FileExists(provider.LaunchSettingsFile));
            AssertEx.CollectionLength(provider.CurrentSnapshot.GlobalSettings, 2);

            // Check snapshot
            Assert.True(provider.CurrentSnapshot.GlobalSettings.TryGetValue("iisSettings", out object? updatedSettings));
            Assert.True(((IISSettingsData)updatedSettings!).WindowsAuthentication);
            Assert.True(((IVersionedLaunchSettings)provider.CurrentSnapshot).Version >= 123);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UpdateGlobalSettingAsync_SettingDoesntExist(bool isInMemory)
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            SetJsonSerializationProviders(provider);

            var globalSettings = ImmutableStringDictionary<object>.EmptyOrdinal.Add("test", new LaunchProfile());

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.GlobalSettings).Returns(globalSettings);
            testSettings.Setup(m => m.Profiles).Returns(ImmutableList<ILaunchProfile>.Empty);

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            var newSettings = new IISSettingsData() { WindowsAuthentication = true, DoNotPersist = isInMemory };

            await provider.UpdateGlobalSettingsAsync(existing => {
                var updates = ImmutableDictionary<string, object?>.Empty
                    .Add("iisSettings", newSettings);
                return updates;
            });

            // Check disk file was written
            Assert.Equal(!isInMemory, moqFS.FileExists(provider.LaunchSettingsFile));
            AssertEx.CollectionLength(provider.CurrentSnapshot.GlobalSettings, 2);

            // Check snapshot
            Assert.True(provider.CurrentSnapshot.GlobalSettings.TryGetValue("iisSettings", out object? updatedSettings));
            Assert.True(((IISSettingsData)updatedSettings!).WindowsAuthentication);
            Assert.True(((IVersionedLaunchSettings)provider.CurrentSnapshot).Version >= 123);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task AddOrUpdateGlobalSettingAsync_SettingExists(bool isInMemory, bool existingIsInMemory)
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            SetJsonSerializationProviders(provider);

            var globalSettings = ImmutableStringDictionary<object>.EmptyOrdinal
                .Add("test", new LaunchProfile())
                .Add("iisSettings", new IISSettingsData() { DoNotPersist = existingIsInMemory });

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.GlobalSettings).Returns(globalSettings);
            testSettings.Setup(m => m.Profiles).Returns(ImmutableList<ILaunchProfile>.Empty);

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            var newSettings = new IISSettingsData() { WindowsAuthentication = true, DoNotPersist = isInMemory };

            await provider.AddOrUpdateGlobalSettingAsync("iisSettings", newSettings);

            // Check disk file was written
            Assert.Equal(!isInMemory || (isInMemory && !existingIsInMemory), moqFS.FileExists(provider.LaunchSettingsFile));

            // Check snapshot
            AssertEx.CollectionLength(provider.CurrentSnapshot.GlobalSettings, 2);
            Assert.True(provider.CurrentSnapshot.GlobalSettings.TryGetValue("iisSettings", out object? updatedSettings));
            Assert.True(((IISSettingsData)updatedSettings!).WindowsAuthentication);
            Assert.True(((IVersionedLaunchSettings)provider.CurrentSnapshot).Version >= 123);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task UpdateGlobalSettingAsync_SettingExists(bool isInMemory, bool existingIsInMemory)
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            SetJsonSerializationProviders(provider);

            var globalSettings = ImmutableStringDictionary<object>.EmptyOrdinal
                .Add("test", new LaunchProfile())
                .Add("iisSettings", new IISSettingsData() { DoNotPersist = existingIsInMemory });

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.GlobalSettings).Returns(globalSettings);
            testSettings.Setup(m => m.Profiles).Returns(ImmutableList<ILaunchProfile>.Empty);

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            var newSettings = new IISSettingsData() { WindowsAuthentication = true, DoNotPersist = isInMemory };

            await provider.UpdateGlobalSettingsAsync(existing => {
                var updates = ImmutableDictionary<string, object?>.Empty
                    .Add("iisSettings", newSettings);
                return updates;
            });

            // Check disk file was written
            Assert.Equal(!isInMemory || (isInMemory && !existingIsInMemory), moqFS.FileExists(provider.LaunchSettingsFile));

            // Check snapshot
            AssertEx.CollectionLength(provider.CurrentSnapshot.GlobalSettings, 2);
            Assert.True(provider.CurrentSnapshot.GlobalSettings.TryGetValue("iisSettings", out object? updatedSettings));
            Assert.True(((IISSettingsData)updatedSettings!).WindowsAuthentication);
            Assert.True(((IVersionedLaunchSettings)provider.CurrentSnapshot).Version >= 123);
        }

        [Fact]
        public async Task RemoveGlobalSettingAsync_SettingDoesntExist()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            SetJsonSerializationProviders(provider);

            var globalSettings = ImmutableStringDictionary<object>.EmptyOrdinal.Add("test", new LaunchProfile());

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.GlobalSettings).Returns(globalSettings);
            testSettings.Setup(m => m.Profiles).Returns(ImmutableList<ILaunchProfile>.Empty);
            var versionedTestSettings = testSettings.As<IVersionedLaunchSettings>();
            versionedTestSettings.Setup(m => m.Version).Returns(42);

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            await provider.RemoveGlobalSettingAsync("iisSettings");

            // Check disk file was not written
            Assert.False(moqFS.FileExists(provider.LaunchSettingsFile));

            // Check snapshot
            Assert.Single(provider.CurrentSnapshot.GlobalSettings);
            Assert.Equal(42, ((IVersionedLaunchSettings)provider.CurrentSnapshot).Version);
        }

        [Fact]
        public async Task RemoveGlobalSettingAsync_SettingExists()
        {
            var moqFS = new IFileSystemMock();
            using var provider = GetLaunchSettingsProvider(moqFS);
            SetJsonSerializationProviders(provider);

            var globalSettings = ImmutableStringDictionary<object>.EmptyOrdinal
                .Add("test", new LaunchProfile())
                .Add("iisSettings", new IISSettingsData());

            var testSettings = new Mock<ILaunchSettings>();
            testSettings.Setup(m => m.GlobalSettings).Returns(globalSettings);
            testSettings.Setup(m => m.Profiles).Returns(ImmutableList<ILaunchProfile>.Empty);

            provider.SetCurrentSnapshot(testSettings.Object);
            provider.SetNextVersionTest(123);

            await provider.RemoveGlobalSettingAsync("iisSettings");

            // Check disk file was written
            Assert.True(moqFS.FileExists(provider.LaunchSettingsFile));

            // Check snapshot
            Assert.Single(provider.CurrentSnapshot.GlobalSettings);
            Assert.False(provider.CurrentSnapshot.GlobalSettings.TryGetValue("iisSettings", out _));
            Assert.True(((IVersionedLaunchSettings)provider.CurrentSnapshot).Version >= 123);
        }

        private readonly string JsonString1 = @"{
  ""profiles"": {
  ""IIS Express"":
    {
      ""commandName"": ""IISExpress"",
      ""launchUrl"": ""http://localhost:1234:/test.html"",
      ""launchBrowser"": true
    },
    ""HasCustomValues"":
    {
      ""executablePath"": ""c:\\test\\project\\bin\\project.exe"",
      ""workingDirectory"": ""c:\\test\\project"",
      ""commandLineArgs"": ""--arg1 --arg2"",
      ""custom1"": true,
      ""custom2"": 124,
      ""custom3"": ""mycustomVal""
    },
    ""Docker"":
    {
      ""commandName"": ""Docker"",
      ""launchBrowser"": false,
      ""dockerOption1"": ""some option in docker"",
      ""dockerOption2"": ""Another option in docker""
    },
    ""web"":
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
        private readonly string JsonStringWithWebSettings = @"{
  ""iisSettings"": {
    ""windowsAuthentication"": true,
    ""anonymousAuthentication"": false,
    ""iisExpress"": {
      ""applicationUrl"": ""http://localhost:12345/"",
      ""sslPort"": 44301
    }
  },
  ""profiles"": {
    ""IIS Express"": {
      ""commandName"": ""IISExpress"",
      ""launchBrowser"": true
    },
    ""bar"": {
      ""executablePath"": ""c:\\test\\project\\bin\\test.exe"",
      ""commandLineArgs"": ""-someArg""
    }
  }
}";
        private readonly string BadJsonString = @"{
  ""profiles"": {
    {
      ""name"": ""IIS Express"",
      ""launchBrowser"": ""True""
    },
    },
    {
      ""Name"": ""bar"",
      ""launchBrowser"": ""False""
    }
  }
}";
    }

    // Derives from base class to be able to set protected members
    internal class LaunchSettingsUnderTest : LaunchSettingsProvider
    {
        public LaunchSettingsUnderTest(
            UnconfiguredProject project,
            IUnconfiguredProjectServices projectServices,
            IFileSystem fileSystem,
            IUnconfiguredProjectCommonServices commonProjectServices,
            IActiveConfiguredProjectSubscriptionService? projectSubscriptionService,
            IActiveConfiguredValue<IAppDesignerFolderSpecialFileProvider?> appDesignerFolderSpecialFileProvider,
            IProjectFaultHandlerService projectFaultHandler,
            IDefaultLaunchProfileProvider defaultLaunchProfileProvider)
          : base(project, projectServices, fileSystem, commonProjectServices, projectSubscriptionService, appDesignerFolderSpecialFileProvider, projectFaultHandler)
        {
            // Block the code from setting up one on the real file system. Since we block, it we need to set up the fileChange scheduler manually
            FileWatcher = new SimpleFileWatcher();
            // Make the unit tests run faster
            FileChangeProcessingDelay = TimeSpan.FromMilliseconds(50);
            FileChangeScheduler = new TaskDelayScheduler(FileChangeProcessingDelay, commonProjectServices.ThreadingService,
                    CancellationToken.None);
            DefaultLaunchProfileProviders.Add(defaultLaunchProfileProvider);
        }

        // Wrappers to call protected members
        public void SetCurrentSnapshot(ILaunchSettings profiles) { CurrentSnapshot = profiles; }
        public Task<LaunchSettingsData> ReadSettingsFileFromDiskTestAsync() { return ReadSettingsFileFromDiskAsync(); }
        public Task SaveSettingsToDiskAsyncTest(ILaunchSettings curSettings) { return SaveSettingsToDiskAsync(curSettings); }
        public Task UpdateAndSaveSettingsInternalAsyncTest(ILaunchSettings curSettings, bool persistToDisk) { return UpdateAndSaveSettingsInternalAsync(curSettings, persistToDisk); }
        public void SetNextVersionTest(long nextVersion) { SetNextVersion(nextVersion); }

        public DateTime LastSettingsFileSyncTimeTest { get { return LastSettingsFileSyncTimeUtc; } set { LastSettingsFileSyncTimeUtc = value; } }
        public Task UpdateProfilesAsyncTest(string? activeProfile) { return UpdateProfilesAsync(activeProfile); }
        public void SetIgnoreFileChanges(bool value) { IgnoreFileChanges = value; }
        public Task<bool> SettingsFileHasChangedAsyncTest() { return SettingsFileHasChangedAsync(); }
        public Task LaunchSettingsFile_ChangedTest() => HandleLaunchSettingsFileChangedAsync();

        internal void SetSettingsProviderCollection(OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection> settingsProviders)
        {
            JsonSerializationProviders = settingsProviders;
        }

        internal void SetSourceControlProviderCollection(OrderPrecedenceImportCollection<ISourceCodeControlIntegration> sccProviders)
        {
            SourceControlIntegrations = sccProviders;
        }
    }

    // Used to test global settings
    [JsonObject(MemberSerialization.OptIn)]
    internal class ServerBindingData
    {
        [JsonProperty(PropertyName = "applicationUrl")]
        public string? ApplicationUrl { get; set; }

        [JsonProperty(PropertyName = "sslPort")]
        public int SSLPort { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal class IISSettingsData : IPersistOption
    {
        public const bool DefaultAnonymousAuth = true;
        public const bool DefaultWindowsAuth = false;

        [JsonProperty(PropertyName = "windowsAuthentication")]
        public bool WindowsAuthentication { get; set; } = DefaultWindowsAuth;

        [JsonProperty(PropertyName = "anonymousAuthentication")]
        public bool AnonymousAuthentication { get; set; } = DefaultAnonymousAuth;

        [JsonProperty(PropertyName = "iis")]
        public ServerBindingData? IISBindingData { get; set; }

        [JsonProperty(PropertyName = "iisExpress")]
        public ServerBindingData? IISExpressBindingData { get; set; }

        public bool DoNotPersist { get; set; }
    }
}
