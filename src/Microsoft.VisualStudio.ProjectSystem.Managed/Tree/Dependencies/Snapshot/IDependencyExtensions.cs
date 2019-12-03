// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal static class IDependencyExtensions
    {
        /// <summary>
        /// Returns a IDependencyViewModel for given dependency.
        /// </summary>
        public static IDependencyViewModel ToViewModel(this IDependency dependency, TargetedDependenciesSnapshot snapshot)
        {
            bool hasUnresolvedDependency = !dependency.Resolved || snapshot.ShouldAppearUnresolved(dependency);

            return new DependencyViewModel(dependency, hasUnresolvedDependency: hasUnresolvedDependency);
        }

        private sealed class DependencyViewModel : IDependencyViewModel
        {
            private readonly IDependency _model;
            private readonly bool _hasUnresolvedDependency;

            public DependencyViewModel(IDependency dependency, bool hasUnresolvedDependency)
            {
                _model = dependency;
                _hasUnresolvedDependency = hasUnresolvedDependency;
            }

            public IDependency? OriginalModel => _model;
            public string Caption => _model.Caption;
            public string? FilePath => _model.Id;
            public string? SchemaName => _model.SchemaName;
            public string? SchemaItemType => _model.SchemaItemType;
            public int Priority => _model.Priority;
            public ImageMoniker Icon => _hasUnresolvedDependency ? _model.IconSet.UnresolvedIcon : _model.IconSet.Icon;
            public ImageMoniker ExpandedIcon => _hasUnresolvedDependency ? _model.IconSet.UnresolvedExpandedIcon : _model.IconSet.ExpandedIcon;
            public ProjectTreeFlags Flags => _model.Flags;
        }

        /// <summary>
        /// Returns id having full path instead of OriginalItemSpec
        /// </summary>
        public static string GetTopLevelId(this IDependency self)
        {
            return string.IsNullOrEmpty(self.Path)
                ? self.Id
                : Dependency.GetID(self.TargetFramework, self.ProviderType, self.Path);
        }

        /// <summary>
        /// Returns id having full path instead of OriginalItemSpec
        /// </summary>
        public static bool TopLevelIdEquals(this IDependency self, string id)
        {
            return string.IsNullOrEmpty(self.Path)
                ? string.Equals(self.Id, id, StringComparisons.DependencyTreeIds)
                : Dependency.IdEquals(id, self.TargetFramework, self.ProviderType, self.Path);
        }

        /// <summary>
        /// Returns true if given dependency is a nuget package.
        /// </summary>
        public static bool IsPackage(this IDependency self)
        {
            return StringComparers.DependencyProviderTypes.Equals(self.ProviderType, PackageRuleHandler.ProviderTypeString);
        }

        /// <summary>
        /// Returns true if given dependency is a project.
        /// </summary>
        public static bool IsProject(this IDependency self)
        {
            return StringComparers.DependencyProviderTypes.Equals(self.ProviderType, ProjectRuleHandler.ProviderTypeString);
        }

        /// <summary>
        /// Returns true if given dependencies belong to the same targeted snapshot, i.e. have same target.
        /// </summary>
        public static bool HasSameTarget(this IDependency self, IDependency other)
        {
            Requires.NotNull(other, nameof(other));

            return self.TargetFramework.Equals(other.TargetFramework);
        }

        public static IDependency ToResolved(
            this IDependency dependency,
            string? schemaName = null,
            ImmutableArray<string> dependencyIDs = default)
        {
            return dependency.SetProperties(
                resolved: true,
                flags: dependency.GetResolvedFlags(),
                schemaName: schemaName,
                dependencyIDs: dependencyIDs);
        }

        public static IDependency ToUnresolved(
            this IDependency dependency,
            string? schemaName = null,
            ImmutableArray<string> dependencyIDs = default)
        {
            return dependency.SetProperties(
                resolved: false,
                flags: dependency.GetUnresolvedFlags(),
                schemaName: schemaName,
                dependencyIDs: dependencyIDs);
        }

        public static ProjectTreeFlags GetResolvedFlags(this IDependency dependency)
        {
            return dependency.Flags
                .Union(DependencyTreeFlags.Resolved)
                .Except(DependencyTreeFlags.Unresolved);
        }

        public static ProjectTreeFlags GetUnresolvedFlags(this IDependency dependency)
        {
            return dependency.Flags
                .Union(DependencyTreeFlags.Unresolved)
                .Except(DependencyTreeFlags.Resolved);
        }
    }
}
