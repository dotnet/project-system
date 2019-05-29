// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;

#nullable enable

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Provides immutable dependencies snapshot for a given project.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IDependenciesSnapshotProvider
    {
        /// <summary>
        /// Gets the current immutable dependencies snapshot for the project.
        /// </summary>
        /// <remarks>
        /// Never null.
        /// </remarks>
        IDependenciesSnapshot CurrentSnapshot { get; }

        /// <summary>
        /// Dataflow to monitor the project snapshot changes.
        /// </summary>
        IReceivableSourceBlock<SnapshotChangedEventArgs> SnapshotChangedSource { get; }

        /// <summary>
        /// Raised when the project's full path changes (i.e. due to being renamed).
        /// </summary>
        event EventHandler<ProjectRenamedEventArgs> SnapshotRenamed;

        /// <summary>
        /// Raised when the project's dependencies snapshot changed.
        /// </summary>
        event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;

        /// <summary>
        /// Raised when the project and its snapshot provider are unloading.
        /// </summary>
        event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;
    }

    internal sealed class SnapshotChangedEventArgs : EventArgs
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
