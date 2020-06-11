// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    internal static class IDependencyExtensions
    {
        public static IDependency ToResolved(
            this IDependency dependency,
            string? schemaName = null)
        {
            return dependency.SetProperties(
                resolved: true,
                flags: dependency.GetResolvedFlags(),
                schemaName: schemaName);
        }

        public static IDependency ToUnresolved(
            this IDependency dependency,
            string? schemaName = null)
        {
            return dependency.SetProperties(
                resolved: false,
                flags: dependency.GetUnresolvedFlags(),
                schemaName: schemaName);
        }

        private static ProjectTreeFlags GetResolvedFlags(this IDependency dependency)
        {
            return dependency.Flags
                .Union(DependencyTreeFlags.Resolved)
                .Except(DependencyTreeFlags.Unresolved);
        }

        private static ProjectTreeFlags GetUnresolvedFlags(this IDependency dependency)
        {
            return dependency.Flags
                .Union(DependencyTreeFlags.Unresolved)
                .Except(DependencyTreeFlags.Resolved);
        }
    }
}
