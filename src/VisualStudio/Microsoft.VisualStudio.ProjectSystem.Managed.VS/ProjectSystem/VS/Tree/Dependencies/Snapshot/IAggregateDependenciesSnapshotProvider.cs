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
        /// Returns a snapshot provider for given project path.
        /// </summary>
        /// <param name="projectFilePath">Path to project for which snapshot provider is requested</param>
        /// <returns><see cref="IDependenciesSnapshotProvider"/> or null if there no such snapshot provider found.</returns>
        IDependenciesSnapshotProvider GetSnapshotProvider(string projectFilePath);

        /// <summary>
        /// Get all registered snapshot providers.
        /// </summary>
        /// <returns>A collection of <see cref="IDependenciesSnapshotProvider"/></returns>
        IEnumerable<IDependenciesSnapshotProvider> GetSnapshotProviders();

        /// <summary>
        /// Fired when a snapshot chnaged in a snapshot provider
        /// </summary>
        event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;

        /// <summary>
        /// Fired when snapshot provider is unloading
        /// </summary>
        event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;
    }
}
