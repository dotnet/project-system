// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions
{
    internal static class IDependenciesChangesExtensions
    {
        public static bool AnyChanges(this IDependenciesChanges changes)
        {
            return changes.AddedNodes.Any() || changes.RemovedNodes.Any();
        }
    }
}
