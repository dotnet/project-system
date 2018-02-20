// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal static class IDependencyExtensions
    {
        /// <summary>
        /// Specifies if there is unresolved child somewhere in the dependency graph
        /// </summary>
        public static bool HasUnresolvedDependency(this IDependency self, ITargetedDependenciesSnapshot snapshot)
        {
            return snapshot.CheckForUnresolvedDependencies(self);
        }


        /// <summary>
        /// Returns true if this reference itself is unresolved or it has at least 
        /// one unresolved reference somewhere in the dependency chain.
        /// </summary>
        public static bool IsOrHasUnresolvedDependency(this IDependency self, ITargetedDependenciesSnapshot snapshot)
        {
            return !self.Resolved || self.HasUnresolvedDependency(snapshot);
        }

        /// <summary>
        /// Returns a IDependencyViewModel for given dependency.
        /// </summary>
        public static IDependencyViewModel ToViewModel(this IDependency self, ITargetedDependenciesSnapshot snapshot)
        {
            return new DependencyViewModel
            {
                Caption = self.Caption,
                FilePath = self.Id,
                SchemaName = self.SchemaName,
                SchemaItemType = self.SchemaItemType,
                Priority = self.Priority,
                Icon = self.IsOrHasUnresolvedDependency(snapshot) ? self.UnresolvedIcon : self.Icon,
                ExpandedIcon = self.IsOrHasUnresolvedDependency(snapshot) ? self.UnresolvedExpandedIcon : self.ExpandedIcon,
                Properties = self.Properties,
                Flags = self.Flags,
                OriginalModel = self
            };
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
        /// Returns all icons specified for given dependency.
        /// </summary>
        public static IEnumerable<ImageMoniker> GetIcons(this IDependency self)
        {
            yield return self.Icon;
            yield return self.ExpandedIcon;
            yield return self.UnresolvedIcon;
            yield return self.UnresolvedExpandedIcon;
        }

        /// <summary>
        /// Returns true if given dependency is a nuget package.
        /// </summary>
        public static bool IsPackage(this IDependency self)
        {
            return self.ProviderType.Equals(PackageRuleHandler.ProviderTypeString, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if given dependency is a project.
        /// </summary>
        public static bool IsProject(this IDependency self)
        {
            return self.ProviderType.Equals(ProjectRuleHandler.ProviderTypeString, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if given dependencies belong to the same targeted snapshot, i.e. have same target.
        /// </summary>
        public static bool HasSameTarget(this IDependency self, IDependency other)
        {
            Requires.NotNull(other, nameof(other));
            return self.TargetFramework.Equals(other.TargetFramework);
        }

        public static IDependency ToResolved(this IDependency dependency,
                                             string schemaName = null,
                                             IImmutableList<string> dependencyIDs = null)
        {
            return dependency.SetProperties(
                resolved: true,
                flags: dependency.GetResolvedFlags(),
                schemaName: schemaName,
                dependencyIDs: dependencyIDs);
        }

        public static IDependency ToUnresolved(this IDependency dependency,
                                               string schemaName = null,
                                               IImmutableList<string> dependencyIDs = null)
        {
            return dependency.SetProperties(
                resolved: false,
                flags: dependency.GetUnresolvedFlags(),
                schemaName: schemaName,
                dependencyIDs: dependencyIDs);
        }

        public static ProjectTreeFlags GetResolvedFlags(this IDependency dependency)
        {
            return dependency.Flags.Union(DependencyTreeFlags.ResolvedFlags)
                                   .Except(DependencyTreeFlags.UnresolvedFlags);
        }

        public static ProjectTreeFlags GetUnresolvedFlags(this IDependency dependency)
        {
            return dependency.Flags.Union(DependencyTreeFlags.UnresolvedFlags)
                                   .Except(DependencyTreeFlags.ResolvedFlags);
        }
    }
}
