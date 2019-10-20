// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Global scope contract that provides information about project level 
    /// dependencies snapshot providers.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IAggregateDependenciesSnapshotProvider
    {
        /// <summary>
        /// Since IAggregateDependenciesSnapshotProvider is global scope component, 
        /// each <see cref="DependenciesSnapshotProvider"/> should register itself when it is ready.
        /// </summary>
        /// <returns>An object that, when disposed, unregisters <paramref name="snapshotProvider"/>.</returns>
        IDisposable RegisterSnapshotProvider(DependenciesSnapshotProvider snapshotProvider);

        /// <summary>
        /// Gets the snapshot for <paramref name="projectFilePath"/>, or <see langword="null"/> if none found.
        /// </summary>
        /// <param name="projectFilePath">Path to the project for which the snapshot is requested.</param>
        /// <returns><see cref="DependenciesSnapshot"/> or <see langword="null"/> if no project exists with the specified path.</returns>
        DependenciesSnapshot? GetSnapshot(string projectFilePath);

        /// <summary>
        /// Gets the targeted snapshot for <paramref name="dependency"/>, or <see langword="null"/> if none found.
        /// </summary>
        /// <param name="dependency">A dependency that identifies the project and target framework to search with.</param>
        /// <returns><see cref="TargetedDependenciesSnapshot"/> or <see langword="null"/> if no snapshot exists with matching project and target framework.</returns>
        TargetedDependenciesSnapshot? GetSnapshot(IDependency dependency);

        /// <summary>
        /// Gets the current snapshot from every registered project.
        /// </summary>
        /// <returns>A collection of <see cref="DependenciesSnapshot"/>. Will not contain <see langword="null"/> values.</returns>
        IReadOnlyCollection<DependenciesSnapshot> GetSnapshots();

        /// <summary>
        /// Fired when a snapshot changed in a snapshot provider.
        /// </summary>
        event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;

        /// <summary>
        /// Fired when snapshot provider is unloading.
        /// </summary>
        event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;
    }
}
