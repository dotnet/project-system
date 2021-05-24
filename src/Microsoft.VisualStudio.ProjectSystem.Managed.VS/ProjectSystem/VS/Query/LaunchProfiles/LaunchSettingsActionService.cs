// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    [Export(typeof(ILaunchSettingsActionService))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class LaunchSettingsActionService : ILaunchSettingsActionService
    {
        private readonly ILaunchSettingsProvider _launchSettingsProvider;

        [ImportingConstructor]
        public LaunchSettingsActionService(ILaunchSettingsProvider launchSettingsProvider)
        {
            _launchSettingsProvider = launchSettingsProvider;
        }

        public async Task<ILaunchProfile?> AddLaunchProfileAsync(string commandName, string? newProfileName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            newProfileName ??= await GetNewProfileNameAsync(cancellationToken);

            await _launchSettingsProvider.AddOrUpdateProfileAsync(
                new WritableLaunchProfile
                {
                    Name = newProfileName,
                    CommandName = commandName
                }.ToLaunchProfile(),
                addToFront: false);

            return _launchSettingsProvider.CurrentSnapshot?.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(newProfileName, p.Name));
        }

        public async Task<ILaunchProfile?> DuplicateLaunchProfileAsync(string currentProfileName, string? newProfileName, string? newProfileCommandName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ILaunchSettings? launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite).WithCancellation(cancellationToken);
            Assumes.NotNull(launchSettings);

            ILaunchProfile? existingProfile = launchSettings.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(p.Name, currentProfileName));
            if (existingProfile is not null)
            {
                newProfileName ??= await GetNewProfileNameAsync(cancellationToken);
                newProfileCommandName ??= existingProfile.CommandName;

                var writableProfile = new WritableLaunchProfile(existingProfile);
                writableProfile.Name = newProfileName;
                writableProfile.CommandName = newProfileCommandName;

                await _launchSettingsProvider.AddOrUpdateProfileAsync(writableProfile.ToLaunchProfile(), addToFront: false);

                return _launchSettingsProvider.CurrentSnapshot?.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(newProfileName, p.Name));
            }

            return null;
        }

        public Task RemoveLaunchProfileAsync(string profileName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _launchSettingsProvider.RemoveProfileAsync(profileName);
        }

        public async Task<ILaunchProfile?> RenameLaunchProfileAsync(string currentProfileName, string newProfileName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ILaunchSettings? launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite).WithCancellation(cancellationToken);
            Assumes.NotNull(launchSettings);

            ILaunchProfile? existingProfile = launchSettings.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(p.Name, currentProfileName));
            if (existingProfile is not null)
            {
                var writableProfile = new WritableLaunchProfile(existingProfile);
                writableProfile.Name = newProfileName;

                await _launchSettingsProvider.RemoveProfileAsync(currentProfileName);
                await _launchSettingsProvider.AddOrUpdateProfileAsync(writableProfile.ToLaunchProfile(), addToFront: false);

                return _launchSettingsProvider.CurrentSnapshot?.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(newProfileName, p.Name));
            }

            return null;
        }

        private async Task<string> GetNewProfileNameAsync(CancellationToken cancellationToken = default)
        {
            ILaunchSettings? launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite).WithCancellation(cancellationToken);
            Assumes.NotNull(launchSettings);

            string? newProfileName = null;
            for (int i = 1; newProfileName is null; i++)
            {
                string potentialProfileName = string.Format(VSResources.DefaultNewProfileName, i);
                if (!launchSettings.Profiles.Any(profile => StringComparers.LaunchProfileNames.Equals(potentialProfileName, profile.Name)))
                {
                    newProfileName = potentialProfileName;
                }
            }

            return newProfileName;
        }
    }
}
