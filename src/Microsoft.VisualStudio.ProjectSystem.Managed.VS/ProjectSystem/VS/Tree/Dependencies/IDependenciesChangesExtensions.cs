// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal static class IDependenciesChangesExtensions
    {
        public static bool AnyChanges(this IDependenciesChanges self)
        {
            return self.AddedNodes.Count > 0 || self.RemovedNodes.Count > 0;
        }
    }
}
