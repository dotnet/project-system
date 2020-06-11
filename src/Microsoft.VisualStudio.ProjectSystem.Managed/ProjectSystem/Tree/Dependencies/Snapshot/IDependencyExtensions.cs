// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    internal static class IDependencyExtensions
    {
        /// <summary>
        /// Returns a IDependencyViewModel for given dependency.
        /// </summary>
        public static IDependencyViewModel ToViewModel(this IDependency dependency)
        {
            return new DependencyViewModel(dependency);
        }

        private sealed class DependencyViewModel : IDependencyViewModel
        {
            private readonly IDependency _dependency;
            private readonly bool _hasUnresolvedDependency;

            public DependencyViewModel(IDependency dependency)
            {
                Requires.NotNull(dependency, nameof(dependency));
                Requires.Argument(dependency.Visible, nameof(dependency), "Must be visible");

                _dependency = dependency;
                _hasUnresolvedDependency = !dependency.Resolved;
            }

            public string Caption => _dependency.Caption;
            public string? FilePath => _dependency.Path;
            public string? SchemaName => _dependency.SchemaName;
            public string? SchemaItemType => _dependency.SchemaItemType;
            public ImageMoniker Icon => _hasUnresolvedDependency ? _dependency.IconSet.UnresolvedIcon : _dependency.IconSet.Icon;
            public ImageMoniker ExpandedIcon => _hasUnresolvedDependency ? _dependency.IconSet.UnresolvedExpandedIcon : _dependency.IconSet.ExpandedIcon;
            public ProjectTreeFlags Flags => _dependency.Flags;
        }

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
