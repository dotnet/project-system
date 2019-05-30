// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal static class IDependenciesChangesExtensions
    {
        public static bool AnyChanges(this IDependenciesChanges changes)
        {
            return changes.AddedNodes.Any() || changes.RemovedNodes.Any();
        }
    }
}
