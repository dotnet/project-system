// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Aggregates version information from the multiple <see cref="LaunchSettingsTracker" />s
    /// and passes it on to the bound <see cref="ILaunchSettingsVersionPublisher"/>, if any.
    /// </summary>
    /// <remarks>
    /// The <see cref="LaunchSettingsTracker"/> loads automatically for any project
    /// supporting launch profiles. However, we don't want to load all of the assemblies
    /// associated with the Project Query API until they are actually needed, so <see cref="LaunchSettingsTracker"/>
    /// can't refer to any of them directly. Instead, it communicates with this type,
    /// which will pass on the version information only after being bound to an <see cref="ILaunchSettingsVersionPublisher"/>.
    /// </remarks>
    [Export]
    internal sealed class LaunchSettingsQueryVersionProvider
    {
        private readonly object _lock = new();

        private ImmutableDictionary<string, long> _versions = ImmutableStringDictionary<long>.EmptyOrdinal;

        private ILaunchSettingsVersionPublisher? _versionPublisher;

        /// <summary>
        /// Sets the related <see cref="ILaunchSettingsVersionPublisher"/> and pushes
        /// the current version information to it.
        /// </summary>
        internal void BindToVersionPublisher(ILaunchSettingsVersionPublisher versionPublisher)
        {
            lock (_lock)
            {
                _versionPublisher = versionPublisher;
                _versionPublisher.UpdateVersions(_versions);
            }
        }

        /// <summary>
        /// Called when the <paramref name="tracker"/> becomes active.
        /// </summary>
        internal void OnLaunchSettingsTrackerActivated(LaunchSettingsTracker tracker)
        {
            lock (_lock)
            {
                string key = tracker.VersionKey;

                _versions = _versions.SetItem(key, tracker.CurrentVersion);

                _versionPublisher?.UpdateVersions(_versions);
            }
        }

        /// <summary>
        /// Called when the <paramref name="tracker"/> has an updated version.
        /// </summary>
        internal void OnLaunchSettingsVersionUpdated(LaunchSettingsTracker tracker)
        {
            lock (_lock)
            {
                _versions = _versions.SetItem(tracker.VersionKey, tracker.CurrentVersion);
                _versionPublisher?.UpdateVersions(_versions);
            }
        }

        /// <summary>
        /// Called when the <paramref name="tracker"/> is deactivated.
        /// </summary>
        internal void OnLaunchSettingsTrackerDeactivated(LaunchSettingsTracker tracker)
        {
            lock (_lock)
            {
                string key = tracker.VersionKey;

                _versions = _versions.Remove(key);

                _versionPublisher?.UpdateVersions(_versions);
            }
        }
    }
}
