// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

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
        /// <remarks>
        /// Never null.
        /// </remarks>
        IDependenciesSnapshot CurrentSnapshot { get; }

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

    internal sealed class SnapshotChangedEventArgs
    {
        public SnapshotChangedEventArgs(IDependenciesSnapshot snapshot, CancellationToken token)
        {
            Requires.NotNull(snapshot, nameof(snapshot));

            Snapshot = snapshot;
            Token = token;
        }

        public IDependenciesSnapshot Snapshot { get; }
        public CancellationToken Token { get; }
    }

    internal sealed class SnapshotProviderUnloadingEventArgs : EventArgs
    {
        public SnapshotProviderUnloadingEventArgs(IDependenciesSnapshotProvider snapshotProvider, CancellationToken token = default)
        {
            Requires.NotNull(snapshotProvider, nameof(snapshotProvider));

            SnapshotProvider = snapshotProvider;
            Token = token;
        }

        public IDependenciesSnapshotProvider SnapshotProvider { get; }
        public CancellationToken Token { get; }
    }
}
