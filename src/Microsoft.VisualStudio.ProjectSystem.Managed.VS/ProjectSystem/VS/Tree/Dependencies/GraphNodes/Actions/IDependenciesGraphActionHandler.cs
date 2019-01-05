// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    /// <summary>
    /// Handles graph requests and/or changes in an <see cref="IDependenciesSnapshot"/>
    /// and updates the corresponding graph (via an <see cref="IGraphContext"/>) appropriately.
    /// </summary>
    internal interface IDependenciesGraphActionHandler
    {
        bool CanHandleRequest(IGraphContext graphContext);
        bool CanHandleChanges();
        bool HandleRequest(IGraphContext graphContext);
        bool HandleChanges(IGraphContext graphContext, SnapshotChangedEventArgs e);
    }
}
