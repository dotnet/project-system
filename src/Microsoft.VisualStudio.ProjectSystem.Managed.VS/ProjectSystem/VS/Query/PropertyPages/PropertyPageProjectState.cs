// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal class PropertyPageProjectState : AbstractProjectState
    {
        public PropertyPageProjectState(UnconfiguredProject project)
            : base(project)
        {
        }

        public override async Task<(string versionKey, long versionNumber)?> GetDataVersionAsync(ProjectConfiguration configuration)
        {
            ConfiguredProject configuredProject = await Project.LoadConfiguredProjectAsync(configuration);
            configuredProject.GetQueryDataVersion(out string versionKey, out long versionNumber);
            return (versionKey, versionNumber);
        }
    }

    internal class LaunchProfileProjectState : AbstractProjectState
    {
        private readonly ILaunchSettingsProvider _launchSettingsProvider;
        private readonly LaunchSettingsTracker _launchSettingsTracker;

        public LaunchProfileProjectState(UnconfiguredProject project, ILaunchSettingsProvider launchSettingsProvider, LaunchSettingsTracker launchSettingsTracker)
            : base(project)
        {
            _launchSettingsProvider = launchSettingsProvider;
            _launchSettingsTracker = launchSettingsTracker;
        }

        public override Task<(string versionKey, long versionNumber)?> GetDataVersionAsync(ProjectConfiguration configuration)
        {
            if (_launchSettingsProvider.CurrentSnapshot is IVersionedLaunchSettings versionedLaunchSettings)
            {
                return Task.FromResult<(string, long)?>((_launchSettingsTracker.VersionKey, versionedLaunchSettings.Version));
            }

            return Task.FromResult<(string, long)?>(null);
        }
    }
}
