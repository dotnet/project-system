// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class WritableLaunchSettings : IWritableLaunchSettings
    {
        public WritableLaunchSettings(ILaunchSettings settings)
        {
            if (settings.Profiles is not null)
            {
                foreach (ILaunchProfile profile in settings.Profiles)
                {
                    // Make a mutable/writable copy of each profile
                    Profiles.Add(new WritableLaunchProfile(profile));
                }
            }

            foreach ((string key, object value) in LaunchSettings.CloneGlobalSettingsValues(settings.GlobalSettings))
            {
                GlobalSettings.Add(key, value);
            }

            if (settings.ActiveProfile is not null)
            {
                ActiveProfile = Profiles.Find(profile => LaunchProfile.IsSameProfileName(profile.Name, settings.ActiveProfile.Name));
            }
        }

        public IWritableLaunchProfile? ActiveProfile { get; set; }

        public List<IWritableLaunchProfile> Profiles { get; } = new List<IWritableLaunchProfile>();

        public Dictionary<string, object> GlobalSettings { get; } = new Dictionary<string, object>(StringComparers.LaunchProfileProperties);

        public ILaunchSettings ToLaunchSettings()
        {
            return new LaunchSettings(
                profiles: Profiles.Select(static profile => profile.ToLaunchProfile()),
                globalSettings: ImmutableStringDictionary<object>.EmptyOrdinal.AddRange(LaunchSettings.CloneGlobalSettingsValues(GlobalSettings)));
        }
    }
}
