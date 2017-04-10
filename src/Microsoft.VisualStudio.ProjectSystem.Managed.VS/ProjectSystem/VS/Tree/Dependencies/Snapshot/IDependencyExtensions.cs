// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal static class IDependencyExtensions
    {
        /// <summary>
        /// Returns true if this reference itself is unresolved or it has at least 
        /// one unresolved reference somewhere in the dependency chain.
        /// </summary>
        public static bool IsOrHasUnresolvedDependency(this IDependency self)
        {
            return !self.Resolved || self.HasUnresolvedDependency;
        }

        /// <summary>
        /// Returns a IDependencyViewModel for given dependency.
        /// </summary>
        public static IDependencyViewModel ToViewModel(this IDependency self)
        {
            return new DependencyViewModel
            {
                Caption = self.Caption,
                FilePath = self.Id,
                SchemaName = self.SchemaName,
                SchemaItemType = self.SchemaItemType,
                Priority = self.Priority,
                Icon = self.IsOrHasUnresolvedDependency() ? self.UnresolvedIcon : self.Icon,
                ExpandedIcon = self.IsOrHasUnresolvedDependency() ? self.UnresolvedExpandedIcon : self.ExpandedIcon,
                Properties = self.Properties,
                Flags = self.Flags
            };
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
            return self.Snapshot.TargetFramework.Equals(other.Snapshot.TargetFramework);
        }

        /// <summary>
        /// Returns true if "other dependency" is a child of given dpendency at any level.
        /// </summary>
        public static bool Contains(this IDependency self, IDependency other)
        {
            return ContainsDependency(self, other);
        }

        private static bool ContainsDependency(this IDependency self, IDependency other)
        {
            var result = self.DependencyIDs.Any(x => x.Equals(other.Id, StringComparison.OrdinalIgnoreCase));
            if (result)
            {
                return true;
            }

            foreach(var dependency in self.Dependencies)
            {
                result = ContainsDependency(dependency, other);
                if (result)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Tries to convert OriginalItemSpec to absolute path for given dependency. If OriginalItemSpec is 
        /// absolute path, just returns OriginalItemSpec. If OriginalItemSpec is not absoulte, tries to make
        /// OriginalItemSpec rooted to current project folder.
        /// </summary>
        public static string GetActualPath(this IDependency dependency, string containingProjectPath)
        {
            var dependencyProjectPath = dependency.OriginalItemSpec;
            if (string.IsNullOrEmpty(dependency.OriginalItemSpec))
            {
                return null;
            }

            if (!ManagedPathHelper.IsRooted(dependency.OriginalItemSpec))
            {
                var projectFolder = Path.GetDirectoryName(containingProjectPath);
                dependencyProjectPath = ManagedPathHelper.TryMakeRooted(projectFolder, dependency.OriginalItemSpec);
            }

            return dependencyProjectPath;
        }
    }
}
