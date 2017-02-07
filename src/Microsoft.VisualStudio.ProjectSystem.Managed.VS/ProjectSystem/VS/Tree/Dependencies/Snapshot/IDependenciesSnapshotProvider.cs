// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal interface IDependenciesSnapshotProvider
    {
        IDependenciesSnapshot CurrentSnapshot { get; }

        string ProjectFilePath { get; }

        event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;

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
