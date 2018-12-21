// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Global scope contract that provides information about project level 
    /// dependencies snapshot providers.
    /// </summary>
    internal interface IAggregateDependenciesSnapshotProvider
    {
        /// <summary>
        /// Since IAggregateDependenciesSnapshotProvider is global scope component, 
        /// each <see cref="IDependenciesSnapshotProvider"/> should register itself when it is ready.
        /// </summary>
        void RegisterSnapshotProvider(IDependenciesSnapshotProvider snapshotProvider);

        /// <summary>
        /// Gets the snapshot for <paramref name="projectFilePath"/>, or <see langword="null"/> if none found.
        /// </summary>
        /// <param name="projectFilePath">Path to the project for which the snapshot is requested.</param>
        /// <returns><see cref="IDependenciesSnapshot"/> or <see langword="null"/> if no project exists with the specified path.</returns>
        IDependenciesSnapshot GetSnapshot(string projectFilePath);

        /// <summary>
        /// Gets the targeted snapshot for <paramref name="dependency"/>, or <see langword="null"/> if none found.
        /// </summary>
        /// <param name="dependency">A dependency that identifies the project and target framework to search with.</param>
        /// <returns><see cref="ITargetedDependenciesSnapshot"/> or <see langword="null"/> if no snapshot exists with matching project and target framework.</returns>
        ITargetedDependenciesSnapshot GetSnapshot(IDependency dependency);

        /// <summary>
        /// Get the current snapshot from every registered project.
        /// </summary>
        /// <returns>A collection of <see cref="IDependenciesSnapshot"/>. Will not contain <see langword="null"/> values.</returns>
        IReadOnlyCollection<IDependenciesSnapshot> GetSnapshots();

        /// <summary>
        /// Fired when a snapshot changed in a snapshot provider
        /// </summary>
        event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;

        /// <summary>
        /// Fired when snapshot provider is unloading
        /// </summary>
        event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;
    }
}
