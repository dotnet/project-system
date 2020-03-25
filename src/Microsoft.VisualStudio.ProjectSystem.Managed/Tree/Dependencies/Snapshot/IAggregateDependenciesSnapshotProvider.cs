// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
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
        /// Gets the targeted snapshot for <paramref name="dependency"/>, or <see langword="null"/> if none found.
        /// </summary>
        /// <param name="dependency">A dependency that identifies the project and target framework to search with.</param>
        /// <returns><see cref="TargetedDependenciesSnapshot"/> or <see langword="null"/> if no snapshot exists with matching project and target framework.</returns>
        TargetedDependenciesSnapshot? GetSnapshot(IDependency dependency);

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
