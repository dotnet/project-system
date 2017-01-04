// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using NuGet.SolutionRestoreManager;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal static class ProjectRestoreInfoBuilder
    {
        private const string DefiningProjectDirectoryProperty = "DefiningProjectDirectory";
        private const string ProjectFileFullPathProperty = "ProjectFileFullPath";

        internal static IVsProjectRestoreInfo Build(IEnumerable<IProjectValueVersions> updates, 
            UnconfiguredProject project)
        {
            Requires.NotNull(updates, nameof(updates));
            Requires.NotNull(project, nameof(project));

            return Build(updates.Cast<IProjectVersionedValue<IProjectSubscriptionUpdate>>(), project);
        }

        internal static IVsProjectRestoreInfo Build(IEnumerable<IProjectVersionedValue<IProjectSubscriptionUpdate>> updates,
            UnconfiguredProject project)
        {
            Requires.NotNull(updates, nameof(updates));
            Requires.NotNull(project, nameof(project));

            // if none of the underlying subscriptions have any changes
            if (!updates.Any(u => u.Value.ProjectChanges.Any(c => c.Value.Difference.AnyChanges)))
            {
                return null;
            }

            string baseIntermediatePath = null;
            string originalTargetFrameworks = null;
            var targetFrameworks = new TargetFrameworks();
            var toolReferences = new ReferenceItems();

            foreach (IProjectVersionedValue<IProjectSubscriptionUpdate> update in updates)
            {
                var nugetRestoreChanges = update.Value.ProjectChanges[NuGetRestore.SchemaName];
                baseIntermediatePath = baseIntermediatePath ??
                    nugetRestoreChanges.After.Properties[NuGetRestore.BaseIntermediateOutputPathProperty];
                originalTargetFrameworks = originalTargetFrameworks ??
                    nugetRestoreChanges.After.Properties[NuGetRestore.TargetFrameworksProperty];
                bool noTargetFramework = 
                    !update.Value.ProjectConfiguration.Dimensions.TryGetValue(NuGetRestore.TargetFrameworkProperty, out string targetFramework) &&
                    !nugetRestoreChanges.After.Properties.TryGetValue(NuGetRestore.TargetFrameworkProperty, out targetFramework);

                if (noTargetFramework || string.IsNullOrEmpty(targetFramework))
                {
                    TraceUtilities.TraceWarning("Unable to find TargetFramework Property");
                    continue;
                }

                if (!targetFrameworks.Contains(targetFramework))
                {
                    var projectReferencesChanges = update.Value.ProjectChanges[ProjectReference.SchemaName];
                    var packageReferencesChanges = update.Value.ProjectChanges[PackageReference.SchemaName];

                    targetFrameworks.Add(new TargetFrameworkInfo
                    {
                        TargetFrameworkMoniker = targetFramework,
                        ProjectReferences = GetProjectReferences(projectReferencesChanges.After.Items, project),
                        PackageReferences = GetReferences(packageReferencesChanges.After.Items),
                        Properties = GetProperties(nugetRestoreChanges.After.Properties)
                    });
                }

                var toolReferencesChanges = update.Value.ProjectChanges[DotNetCliToolReference.SchemaName];
                foreach (var item in toolReferencesChanges.After.Items)
                {
                    if (!toolReferences.Contains(item.Key))
                    {
                        toolReferences.Add(GetReferenceItem(item));
                    }
                }
            }

            // return nominate restore information if any target framework entries are found
            return targetFrameworks.Any()
                ? new ProjectRestoreInfo
                {
                    BaseIntermediatePath = baseIntermediatePath,
                    OriginalTargetFrameworks = originalTargetFrameworks,
                    TargetFrameworks = targetFrameworks,
                    ToolReferences = toolReferences
                }
                : null;
        }

        private static IVsProjectProperties GetProperties(IImmutableDictionary<string, string> items)
        {
            return new ProjectProperties(items.Select(v => new ProjectProperty
            {
                Name = v.Key,
                Value = v.Value
            }));
        }

        private static IVsReferenceItem GetReferenceItem(KeyValuePair<string, IImmutableDictionary<string, string>> item)
        {
            return new ReferenceItem
            {
                Name = item.Key,
                Properties = new ReferenceProperties(item.Value.Select(v => new ReferenceProperty
                {
                    Name = v.Key,
                    Value = v.Value
                }))
            };
        }

        private static IVsReferenceItems GetReferences(IImmutableDictionary<string, IImmutableDictionary<string, string>> items)
        {            
            return new ReferenceItems(items.Select(p => GetReferenceItem(p)));
        }

        private static IVsReferenceItems GetProjectReferences(
            IImmutableDictionary<string, IImmutableDictionary<string, string>> projectReferenceItems,
            UnconfiguredProject project)
        {
            var referenceItems = GetReferences(projectReferenceItems);

            // compute project file full path property for each reference
            foreach (ReferenceItem item in referenceItems)
            {                
                var definingProjectDirectory = item.Properties.Item(DefiningProjectDirectoryProperty);
                var projectFileFullPath = definingProjectDirectory != null 
                    ? MakeRooted(definingProjectDirectory.Value, item.Name)
                    : project.MakeRooted(item.Name);

                ((ReferenceProperties)item.Properties).Add(new ReferenceProperty
                {
                    Name = ProjectFileFullPathProperty, Value = projectFileFullPath
                });
            }

            return referenceItems;
        }

        private static string MakeRooted(string basePath, string path)
        {
            basePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return PathHelper.MakeRooted(basePath + Path.DirectorySeparatorChar, path);
        }
    }
}
