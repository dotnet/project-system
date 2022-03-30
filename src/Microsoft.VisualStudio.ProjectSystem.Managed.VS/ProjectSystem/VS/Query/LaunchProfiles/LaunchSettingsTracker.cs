// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Tracks the launch settings version for a particular project, and reports the version information to the <see cref="LaunchSettingsQueryVersionProvider"/>.
    /// </summary>
    /// <remarks>
    /// Note that as an <see cref="IProjectDynamicLoadComponent"/> the <see cref="LoadAsync"/>
    /// and <see cref="UnloadAsync"/> may be called multiple times as the project is
    /// loaded, unloaded, and as the capabilities change.
    /// </remarks>
    [Export(typeof(LaunchSettingsTracker))]
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class LaunchSettingsTracker : IProjectDynamicLoadComponent
    {
        /// <remarks>
        /// Ensures each <see cref="LaunchSettingsTracker"/> is given a unique version key.
        /// </remarks>
        private static int s_nextTrackerId;

        private readonly UnconfiguredProject _project;
        private readonly ILaunchSettingsProvider _launchSettingsProvider;
        private readonly LaunchSettingsQueryVersionProvider _versionProvider;

        private string? _versionKey;
        private IDisposable? _launchSettingsProviderLink;

        [ImportingConstructor]
        public LaunchSettingsTracker(
            UnconfiguredProject project,
            ILaunchSettingsProvider launchSettingsProvider,
            LaunchSettingsQueryVersionProvider versionProvider)
        {
            _project = project;
            _launchSettingsProvider = launchSettingsProvider;
            _versionProvider = versionProvider;
        }

        public Task LoadAsync()
        {
            _launchSettingsProviderLink = _launchSettingsProvider.SourceBlock.LinkToAction(OnLaunchSettingsChanged, _project);

            ILaunchSettings? currentSnapshot = _launchSettingsProvider.CurrentSnapshot;
            if (currentSnapshot is IVersionedLaunchSettings versionedSettings)
            {
                CurrentVersion = versionedSettings.Version;
                _versionProvider.OnLaunchSettingsTrackerActivated(this);
            }

            return Task.CompletedTask;
        }

        public Task UnloadAsync()
        {
            if (_launchSettingsProviderLink is not null)
            {
                _launchSettingsProviderLink.Dispose();
                _launchSettingsProviderLink = null;
            }

            _versionProvider.OnLaunchSettingsTrackerDeactivated(this);

            return Task.CompletedTask;
        }

        private void OnLaunchSettingsChanged(ILaunchSettings newSettings)
        {
            if (newSettings is IVersionedLaunchSettings versionedSettings)
            {
                CurrentVersion = versionedSettings.Version;
                _versionProvider.OnLaunchSettingsVersionUpdated(this);
            }
        }

        public long CurrentVersion { get; private set; }

        public string VersionKey
        {
            get
            {
                if (_versionKey is null)
                {
                    // Get a unique ID number
                    int trackerIdNumber = Interlocked.Increment(ref s_nextTrackerId);

                    // The project name is only for diagnostic purposes. It is not guaranteed to be unique, 
                    // and it is OK that this won't get updated if the project is renamed.
                    string projectName = Path.GetFileNameWithoutExtension(_project.FullPath);

                    // Assemble the version key.
                    string versionKey = $"LaunchSettings:{trackerIdNumber}:{projectName}";

                    // Ensure that the field is only set once, even when called from multiple threads.
                    Interlocked.CompareExchange(location1: ref _versionKey, value: versionKey, comparand: null);
                }

                return _versionKey!;
            }
        }
    }
}
