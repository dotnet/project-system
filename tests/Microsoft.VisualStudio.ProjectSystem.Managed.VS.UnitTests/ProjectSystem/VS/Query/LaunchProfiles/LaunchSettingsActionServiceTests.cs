// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query.LaunchProfiles
{
    public class LaunchSettingsActionServiceTests
    {
        [Fact]
        public async Task WhenAddingANewProfile_AndAProfileNameIsProvided_TheProvidedNameIsUsed()
        {
            var profiles = new List<ILaunchProfile>();
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                addOrUpdateProfileCallback: (profile, addToFront) => profiles.Add(profile),
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));
            var service = new LaunchSettingsActionService(launchSettingsProvider);

            var newProfile = await service.AddLaunchProfileAsync(commandName: "Alpha", newProfileName: "Beta");

            Assert.NotNull(newProfile);
            Assert.Equal(expected: "Beta", actual: newProfile!.Name);
            Assert.Equal(expected: "Alpha", actual: newProfile!.CommandName);
            Assert.Single(profiles);
        }

        [Fact]
        public async Task WhenAddingANewProfile_AndAProfileNameIsNotProvided_AUniqueNameIsGenerated()
        {
            var profiles = new List<ILaunchProfile>();
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                addOrUpdateProfileCallback: (profile, addToFront) => profiles.Add(profile),
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));
            var service = new LaunchSettingsActionService(launchSettingsProvider);

            var newProfile1 = await service.AddLaunchProfileAsync(commandName: "Alpha", newProfileName: null);

            Assert.NotNull(newProfile1);
            Assert.Equal(expected: "Alpha", actual: newProfile1!.CommandName);
            Assert.NotNull(newProfile1.Name);

            var newProfile2 = await service.AddLaunchProfileAsync(commandName: "Beta", newProfileName: null);

            Assert.NotNull(newProfile2);
            Assert.Equal(expected: "Beta", actual: newProfile2!.CommandName);
            Assert.NotNull(newProfile2.Name);

            Assert.NotEqual(expected: newProfile1.Name, actual: newProfile2.Name);

            Assert.Equal(expected: 2, actual: profiles.Count);
        }

        [Fact]
        public async Task WhenDuplicatingAProfile_AndNameAndCommandAreProvided_TheNameAndCommandAreUsed()
        {
            var profiles = new List<ILaunchProfile>
            {
                new WritableLaunchProfile { Name = "Alpha", CommandName = "Beta", ExecutablePath = @"C:\iguana\aardvark.exe" }.ToLaunchProfile()
            };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: profiles,
                addOrUpdateProfileCallback: (profile, addToFront) => profiles.Add(profile),
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));
            var service = new LaunchSettingsActionService(launchSettingsProvider);

            var duplicatedProfile = await service.DuplicateLaunchProfileAsync(currentProfileName: "Alpha", newProfileName: "Gamma", newProfileCommandName: "Delta");

            Assert.Equal(expected: "Gamma", actual: duplicatedProfile!.Name);
            Assert.Equal(expected: "Delta", actual: duplicatedProfile!.CommandName);
            Assert.Equal(expected: @"C:\iguana\aardvark.exe", actual: duplicatedProfile!.ExecutablePath);
            Assert.Equal(expected: 2, actual: profiles.Count);
        }

        [Fact]
        public async Task WhenDuplicatingAProfile_AndNameAndCommandAreNotProvided_DefaultsAreProvided()
        {
            var profiles = new List<ILaunchProfile>
            {
                new WritableLaunchProfile { Name = "Alpha", CommandName = "Beta", ExecutablePath = @"C:\iguana\aardvark.exe" }.ToLaunchProfile()
            };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: profiles,
                addOrUpdateProfileCallback: (profile, addToFront) => profiles.Add(profile),
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));
            var service = new LaunchSettingsActionService(launchSettingsProvider);

            var duplicatedProfile = await service.DuplicateLaunchProfileAsync(currentProfileName: "Alpha", newProfileName: null, newProfileCommandName: null);

            Assert.NotNull(duplicatedProfile!.Name);
            Assert.NotEqual(expected: "Alpha", actual: duplicatedProfile!.Name);
            Assert.Equal(expected: "Beta", actual: duplicatedProfile!.CommandName);
            Assert.Equal(expected: @"C:\iguana\aardvark.exe", actual: duplicatedProfile!.ExecutablePath);
            Assert.Equal(expected: 2, actual: profiles.Count);
        }

        [Fact]
        public async Task WhenRenamingAProfile_TheOldProfileIsRemovedAndTheNewProfileIsAdded()
        {
            var profiles = new List<ILaunchProfile>
            {
                new WritableLaunchProfile { Name = "Alpha", CommandName = "Beta", ExecutablePath = @"C:\iguana\aardvark.exe" }.ToLaunchProfile()
            };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: profiles,
                addOrUpdateProfileCallback: (profile, addToFront) => profiles.Add(profile),
                removeProfileCallback: (removedProfileName) => profiles.RemoveAll(p => p.Name == removedProfileName),
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));
            var service = new LaunchSettingsActionService(launchSettingsProvider);

            var renamedProfile = await service.RenameLaunchProfileAsync(currentProfileName: "Alpha", newProfileName: "Gamma");

            Assert.Equal(expected: "Gamma", actual: renamedProfile!.Name);
            Assert.Equal(expected: "Beta", actual: renamedProfile!.CommandName);
            Assert.Equal(expected: @"C:\iguana\aardvark.exe", actual: renamedProfile!.ExecutablePath);
            Assert.Single(profiles);
        }

        [Fact]
        public async Task WhenRemovingAProfile_TheProfileIsRemoved()
        {
            var profiles = new List<ILaunchProfile>
            {
                new WritableLaunchProfile { Name = "Alpha", CommandName = "Beta", ExecutablePath = @"C:\iguana\aardvark.exe" }.ToLaunchProfile()
            };
            var launchSettingsProvider = ILaunchSettingsProviderFactory.Create(
                launchProfiles: profiles,
                addOrUpdateProfileCallback: (profile, addToFront) => profiles.Add(profile),
                removeProfileCallback: (removedProfileName) => profiles.RemoveAll(p => p.Name == removedProfileName),
                getProfilesCallback: (p) => ImmutableList.CreateRange(profiles));
            var service = new LaunchSettingsActionService(launchSettingsProvider);

            await service.RemoveLaunchProfileAsync(profileName: "Alpha");

            Assert.Empty(profiles);
        }
    }
}
