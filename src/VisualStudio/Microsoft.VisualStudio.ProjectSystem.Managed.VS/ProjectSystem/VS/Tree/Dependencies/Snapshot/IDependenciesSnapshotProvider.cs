// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Provides immutable dependencies snapshot for given project.
    /// </summary>
    internal interface IDependenciesSnapshotProvider
    {
        /// <summary>
        /// Current immutable dependencies snapshot.
        /// </summary>
        IDependenciesSnapshot CurrentSnapshot { get; }

        /// <summary>
        /// Provider's project path.
        /// </summary>
        string ProjectFilePath { get; }

        /// <summary>
        /// Triggered when snapshot's project was renamed.
        /// </summary>
        event EventHandler<ProjectRenamedEventArgs> SnapshotRenamed;

        /// <summary>
        /// Triggered when snapshot was changed.
        /// </summary>
        event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;

        /// <summary>
        /// Triggered when project and it's dependencies snapshot being unloaded
        /// </summary>
        event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;
    }

    internal class SnapshotChangedEventArgs
    {
        public SnapshotChangedEventArgs(IDependenciesSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public IDependenciesSnapshot Snapshot { get; private set; }
    }

    internal class SnapshotProviderUnloadingEventArgs : EventArgs
    {
        public SnapshotProviderUnloadingEventArgs(IDependenciesSnapshotProvider snapshotProvider)
        {
            SnapshotProvider = snapshotProvider;
        }

        public IDependenciesSnapshotProvider SnapshotProvider { get; }
    }
}
