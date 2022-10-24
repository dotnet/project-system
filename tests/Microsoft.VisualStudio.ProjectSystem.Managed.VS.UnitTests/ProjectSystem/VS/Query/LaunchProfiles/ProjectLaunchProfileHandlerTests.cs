// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query.LaunchProfiles
{
    public class ProjectLaunchProfileHandlerTests
    {
        [Fact]
        public async Task WhenAddingANewProfile_AndAProfileNameIsProvided_TheProvidedNameIsUsed()
        {
            var project = UnconfiguredProjectFactory.Create();
            ILaunchProfile? newProfile = null;

            var profiles = new List<ILaunchProfile>();
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                addOrUpdateProfileCallback: (profile, addToFront) => { profiles.Add(profile); newProfile = profile; },
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));

            var queryVersionProvider = new LaunchSettingsQueryVersionProvider();
            var tracker = new LaunchSettingsTracker(project, launchSettingsProvider, queryVersionProvider);

            var handler = new ProjectLaunchProfileHandler(project, launchSettingsProvider, tracker);

            var context = IQueryExecutionContextFactory.Create();
            var parent = IEntityWithIdFactory.Create("Project", "MyProject");

            var newProfileId = await handler.AddLaunchProfileAsync(context, parent, commandName: "Alpha", newProfileName: "Beta");

            Assert.NotNull(newProfile);
            Assert.Equal(expected: "Beta", actual: newProfile.Name);
            Assert.Equal(expected: "Alpha", actual: newProfile.CommandName);
            Assert.Single(profiles);

            Assert.NotNull(newProfileId);
            Assert.Equal(expected: "LaunchProfile", actual: newProfileId[ProjectModelIdentityKeys.SourceItemType]);
            Assert.Equal(expected: "Beta", actual: newProfileId[ProjectModelIdentityKeys.SourceItemName]);
        }

        [Fact]
        public async Task WhenAddingANewProfile_AndAProfileNameIsNotProvided_AUniqueNameIsGenerated()
        {
            var project = UnconfiguredProjectFactory.Create();
            ILaunchProfile? newProfile = null;

            var profiles = new List<ILaunchProfile>();
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                addOrUpdateProfileCallback: (profile, addToFront) => { profiles.Add(profile); newProfile = profile; },
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));

            var queryVersionProvider = new LaunchSettingsQueryVersionProvider();
            var tracker = new LaunchSettingsTracker(project, launchSettingsProvider, queryVersionProvider);

            var handler = new ProjectLaunchProfileHandler(project, launchSettingsProvider, tracker);

            var context = IQueryExecutionContextFactory.Create();
            var parent = IEntityWithIdFactory.Create("Project", "MyProject");

            var newProfileId = await handler.AddLaunchProfileAsync(context, parent, commandName: "Alpha", newProfileName: null);

            Assert.NotNull(newProfile);
            Assert.Equal(expected: "Alpha", actual: newProfile.CommandName);
            Assert.NotNull(newProfile.Name);

            var firstProfileName = newProfile.Name;

            Assert.NotNull(newProfileId);
            Assert.Equal(expected: "LaunchProfile", actual: newProfileId[ProjectModelIdentityKeys.SourceItemType]);
            Assert.Equal(expected: newProfile.Name, actual: newProfileId[ProjectModelIdentityKeys.SourceItemName]);

            newProfileId = await handler.AddLaunchProfileAsync(context, parent, commandName: "Beta", newProfileName: null);

            Assert.NotNull(newProfile);
            Assert.Equal(expected: "Beta", actual: newProfile.CommandName);
            Assert.NotNull(newProfile.Name);

            var secondProfileName = newProfile.Name;

            Assert.NotNull(newProfileId);
            Assert.Equal(expected: "LaunchProfile", actual: newProfileId[ProjectModelIdentityKeys.SourceItemType]);
            Assert.Equal(expected: newProfile.Name, actual: newProfileId[ProjectModelIdentityKeys.SourceItemName]);

            Assert.NotEqual(firstProfileName, secondProfileName);

            Assert.Equal(expected: 2, actual: profiles.Count);
        }

        [Fact]
        public async Task WhenDuplicatingAProfile_AndNameAndCommandAreProvided_TheNameAndCommandAreUsed()
        {
            var project = UnconfiguredProjectFactory.Create();
            ILaunchProfile? duplicatedProfile = null;

            var profiles = new List<ILaunchProfile>
            {
                new WritableLaunchProfile { Name = "Alpha", CommandName = "Beta", ExecutablePath = @"C:\iguana\aardvark.exe" }.ToLaunchProfile()
            };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: profiles,
                addOrUpdateProfileCallback: (profile, addToFront) => { profiles.Add(profile); duplicatedProfile = profile; },
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));

            var queryVersionProvider = new LaunchSettingsQueryVersionProvider();
            var tracker = new LaunchSettingsTracker(project, launchSettingsProvider, queryVersionProvider);

            var handler = new ProjectLaunchProfileHandler(project, launchSettingsProvider, tracker);

            var context = IQueryExecutionContextFactory.Create();
            var parent = IEntityWithIdFactory.Create("Project", "MyProject");

            var duplicatedProfileId = await handler.DuplicateLaunchProfileAsync(context, parent, currentProfileName: "Alpha", newProfileName: "Gamma", newProfileCommandName: "Delta");

            Assert.Equal(expected: "Gamma", actual: duplicatedProfile!.Name);
            Assert.Equal(expected: "Delta", actual: duplicatedProfile!.CommandName);
            Assert.Equal(expected: @"C:\iguana\aardvark.exe", actual: duplicatedProfile!.ExecutablePath);
            Assert.Equal(expected: 2, actual: profiles.Count);

            Assert.NotNull(duplicatedProfileId);
            Assert.Equal(expected: "LaunchProfile", actual: duplicatedProfileId[ProjectModelIdentityKeys.SourceItemType]);
            Assert.Equal(expected: duplicatedProfile.Name, actual: duplicatedProfileId[ProjectModelIdentityKeys.SourceItemName]);
        }

        [Fact]
        public async Task WhenDuplicatingAProfile_AndNameAndCommandAreNotProvided_DefaultsAreProvided()
        {
            var project = UnconfiguredProjectFactory.Create();
            ILaunchProfile? duplicatedProfile = null;

            var profiles = new List<ILaunchProfile>
            {
                new WritableLaunchProfile { Name = "Alpha", CommandName = "Beta", ExecutablePath = @"C:\iguana\aardvark.exe" }.ToLaunchProfile()
            };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: profiles,
                addOrUpdateProfileCallback: (profile, addToFront) => { profiles.Add(profile); duplicatedProfile = profile; },
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));

            var queryVersionProvider = new LaunchSettingsQueryVersionProvider();
            var tracker = new LaunchSettingsTracker(project, launchSettingsProvider, queryVersionProvider);

            var handler = new ProjectLaunchProfileHandler(project, launchSettingsProvider, tracker);

            var context = IQueryExecutionContextFactory.Create();
            var parent = IEntityWithIdFactory.Create("Project", "MyProject");

            var duplicatedProfileId = await handler.DuplicateLaunchProfileAsync(context, parent, currentProfileName: "Alpha", newProfileName: null, newProfileCommandName: null);

            Assert.NotNull(duplicatedProfile);
            Assert.NotNull(duplicatedProfile.Name);
            Assert.NotEqual(expected: "Alpha", actual: duplicatedProfile.Name);
            Assert.Equal(expected: "Beta", actual: duplicatedProfile.CommandName);
            Assert.Equal(expected: @"C:\iguana\aardvark.exe", actual: duplicatedProfile.ExecutablePath);
            Assert.Equal(expected: 2, actual: profiles.Count);

            Assert.NotNull(duplicatedProfileId);
            Assert.Equal(expected: "LaunchProfile", actual: duplicatedProfileId[ProjectModelIdentityKeys.SourceItemType]);
            Assert.Equal(expected: duplicatedProfile.Name, actual: duplicatedProfileId[ProjectModelIdentityKeys.SourceItemName]);
        }

        [Fact]
        public async Task WhenRenamingAProfile_TheOldProfileIsRemovedAndTheNewProfileIsAdded()
        {
            var project = UnconfiguredProjectFactory.Create();
            ILaunchProfile? addedProfile = null;
            string? removedProfileName = null;

            var profiles = new List<ILaunchProfile>
            {
                new WritableLaunchProfile { Name = "Alpha", CommandName = "Beta", ExecutablePath = @"C:\iguana\aardvark.exe" }.ToLaunchProfile()
            };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: profiles,
                addOrUpdateProfileCallback: (profile, addToFront) => { profiles.Add(profile); addedProfile = profile; },
                removeProfileCallback: (profileName) => { removedProfileName = profileName; profiles.RemoveAll(p => p.Name == profileName); },
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));

            var queryVersionProvider = new LaunchSettingsQueryVersionProvider();
            var tracker = new LaunchSettingsTracker(project, launchSettingsProvider, queryVersionProvider);

            var handler = new ProjectLaunchProfileHandler(project, launchSettingsProvider, tracker);

            var context = IQueryExecutionContextFactory.Create();
            var parent = IEntityWithIdFactory.Create("Project", "MyProject");

            var changes = await handler.RenameLaunchProfileAsync(context, parent, currentProfileName: "Alpha", newProfileName: "Gamma");

            Assert.True(changes.HasValue);

            (EntityIdentity removedProfileId, EntityIdentity addedProfileId) = changes.Value;

            Assert.Equal(expected: "Gamma", actual: addedProfile!.Name);
            Assert.Equal(expected: "Beta", actual: addedProfile!.CommandName);
            Assert.Equal(expected: @"C:\iguana\aardvark.exe", actual: addedProfile!.ExecutablePath);
            Assert.Single(profiles);

            Assert.Equal(expected: "LaunchProfile", actual: removedProfileId![ProjectModelIdentityKeys.SourceItemType]);
            Assert.Equal(expected: "Alpha", actual: removedProfileId![ProjectModelIdentityKeys.SourceItemName]);

            Assert.Equal(expected: "LaunchProfile", actual: addedProfileId![ProjectModelIdentityKeys.SourceItemType]);
            Assert.Equal(expected: "Gamma", actual: addedProfileId![ProjectModelIdentityKeys.SourceItemName]);
        }

        [Fact]
        public async Task WhenRemovingAProfile_TheProfileIsRemoved()
        {
            var project = UnconfiguredProjectFactory.Create();
            string? removedProfileName = null;

            var profiles = new List<ILaunchProfile>
            {
                new WritableLaunchProfile { Name = "Alpha", CommandName = "Beta", ExecutablePath = @"C:\iguana\aardvark.exe" }.ToLaunchProfile()
            };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: profiles,
                removeProfileCallback: (profileName) => { profiles.RemoveAll(p => p.Name == profileName); removedProfileName = profileName; },
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));

            var queryVersionProvider = new LaunchSettingsQueryVersionProvider();
            var tracker = new LaunchSettingsTracker(project, launchSettingsProvider, queryVersionProvider);

            var handler = new ProjectLaunchProfileHandler(project, launchSettingsProvider, tracker);

            var context = IQueryExecutionContextFactory.Create();
            var parent = IEntityWithIdFactory.Create("Project", "MyProject");

            var removedProfileId = await handler.RemoveLaunchProfileAsync(context, parent, profileName: "Alpha");

            Assert.Empty(profiles);
            Assert.Equal(expected: "Alpha", actual: removedProfileName);

            Assert.NotNull(removedProfileId);
            Assert.Equal(expected: "LaunchProfile", actual: removedProfileId[ProjectModelIdentityKeys.SourceItemType]);
            Assert.Equal(expected: "Alpha", actual: removedProfileId[ProjectModelIdentityKeys.SourceItemName]);
        }
    }
}
