// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    internal static class IDependencyExtensions
    {
        public static DependencyId GetDependencyId(this IDependency dependency)
        {
            return new DependencyId(dependency.ProviderType, dependency.Id);
        }
    }
}
