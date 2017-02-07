// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public static class IDependenciesChangeDiffExtensions
    {
        public static bool AnyChanges(this IDependenciesChangeDiff changes)
        {
            return changes.AddedNodes?.Count > 0 
                || changes.UpdatedNodes?.Count > 0 
                || changes.RemovedNodes?.Count > 0;
        }
    }
}
