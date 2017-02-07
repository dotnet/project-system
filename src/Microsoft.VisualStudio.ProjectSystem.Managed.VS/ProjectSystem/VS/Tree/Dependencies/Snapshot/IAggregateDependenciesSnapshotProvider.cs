// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    /// <summary>
    /// Global scope contract that provides information about project level 
    /// dependencies graph contexts.
    /// </summary>
    internal interface IAggregateDependenciesSnapshotProvider
    {
        IDependenciesSnapshotProvider GetSnapshotProvider(string projectFilePath);

        IEnumerable<IDependenciesSnapshotProvider> GetSnapshotProviders();

        event EventHandler<SnapshotChangedEventArgs> SnapshotChanged;

        event EventHandler<SnapshotProviderUnloadingEventArgs> SnapshotProviderUnloading;
    }
}
