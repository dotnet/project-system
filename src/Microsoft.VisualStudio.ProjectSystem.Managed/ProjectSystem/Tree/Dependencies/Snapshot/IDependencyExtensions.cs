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

        public static IDependency ToResolved(
            this IDependency dependency,
            string? schemaName = null,
            DiagnosticLevel? diagnosticLevel = null)
        {
            return dependency.SetProperties(
                resolved: true,
                flags: dependency.GetResolvedFlags(),
                schemaName: schemaName,
                diagnosticLevel: diagnosticLevel);
        }

        public static IDependency ToUnresolved(
            this IDependency dependency,
            string? schemaName = null,
            DiagnosticLevel? diagnosticLevel = null)
        {
            return dependency.SetProperties(
                resolved: false,
                flags: dependency.GetUnresolvedFlags(),
                schemaName: schemaName,
                diagnosticLevel: diagnosticLevel);
        }

        private static ProjectTreeFlags GetResolvedFlags(this IDependency dependency)
        {
            return dependency.Flags
                .Union(ProjectTreeFlags.ResolvedReference)
                .Except(ProjectTreeFlags.BrokenReference);
        }

        private static ProjectTreeFlags GetUnresolvedFlags(this IDependency dependency)
        {
            return dependency.Flags
                .Union(ProjectTreeFlags.BrokenReference)
                .Except(ProjectTreeFlags.ResolvedReference);
        }
    }
}
