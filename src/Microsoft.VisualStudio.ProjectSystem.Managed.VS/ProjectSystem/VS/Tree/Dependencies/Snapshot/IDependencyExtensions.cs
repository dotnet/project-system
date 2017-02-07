// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using System.IO;

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

        public static IEnumerable<ImageMoniker> GetIcons(this IDependency self)
        {
            yield return self.Icon;
            yield return self.ExpandedIcon;
            yield return self.UnresolvedIcon;
            yield return self.UnresolvedExpandedIcon;
        }

        public static bool IsPackage(this IDependency self)
        {
            return self.ProviderType.Equals(PackageRuleHandler.ProviderTypeString, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsProject(this IDependency self)
        {
            return self.ProviderType.Equals(ProjectRuleHandler.ProviderTypeString, StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasSameTarget(this IDependency self, IDependency other)
        {
            Requires.NotNull(other, nameof(other));
            return self.Snapshot.TargetFramework.Equals(other.Snapshot.TargetFramework);
        }

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
