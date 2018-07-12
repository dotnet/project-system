// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    internal interface IDependenciesGraphActionHandler
    {
        bool CanHandleRequest(IGraphContext graphContext);
        bool CanHandleChanges();
        bool HandleRequest(IGraphContext graphContext);
        bool HandleChanges(IGraphContext graphContext, SnapshotChangedEventArgs changes);
    }
}
