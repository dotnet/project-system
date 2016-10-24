// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using NuGet.SolutionRestoreManager;
using System;
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
        private const string TargetFrameworkProperty = "TargetFramework";

        internal static IVsProjectRestoreInfo Build(IEnumerable<IProjectValueVersions> updates)
        {
            Requires.NotNull(updates, nameof(updates));

            return Build(updates.Cast<IProjectVersionedValue<IProjectSubscriptionUpdate>>());
        }

        internal static IVsProjectRestoreInfo Build(IEnumerable<IProjectVersionedValue<IProjectSubscriptionUpdate>> updates)
        {
            Requires.NotNull(updates, nameof(updates));

            // if none of the underlying subscriptions have any changes
            if (!updates.Any(u => u.Value.ProjectChanges.Any(c => c.Value.Difference.AnyChanges)))
            {
                return null;
            }

            string baseIntermediatePath = null;
            var targetFrameworks = new TargetFrameworks();
            var toolReferences = new ReferenceItems();

            foreach (IProjectVersionedValue<IProjectSubscriptionUpdate> update in updates)
            {
                var configurationChanges = update.Value.ProjectChanges[ConfigurationGeneral.SchemaName];
                baseIntermediatePath = baseIntermediatePath ??
                    configurationChanges.After.Properties[ConfigurationGeneral.BaseIntermediateOutputPathProperty];

                string targetFramework; 
                if (!update.Value.ProjectConfiguration.Dimensions.TryGetValue(TargetFrameworkProperty, out targetFramework) &&
                    !configurationChanges.After.Properties.TryGetValue(TargetFrameworkProperty, out targetFramework))
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
                        ProjectReferences = GetProjectReferences(projectReferencesChanges.After.Items),
                        PackageReferences = GetReferences(packageReferencesChanges.After.Items)
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

            return new ProjectRestoreInfo
            {
                BaseIntermediatePath = baseIntermediatePath,
                TargetFrameworks = targetFrameworks,
                ToolReferences = toolReferences
            };
        }

        private static IVsReferenceItem GetReferenceItem(KeyValuePair<String, IImmutableDictionary<String, String>> item)
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

        private static IVsReferenceItems GetReferences(IImmutableDictionary<String, IImmutableDictionary<String, String>> items)
        {
            return new ReferenceItems(items.Select(p => GetReferenceItem(p)));
        }

        private static IVsReferenceItems GetProjectReferences(IImmutableDictionary<String, IImmutableDictionary<String, String>> items)
        {
            var referenceItems = GetReferences(items);
            foreach (ReferenceItem item in referenceItems)
            {
                var definingProjectDirectory = item.Properties.Item(DefiningProjectDirectoryProperty);
                string fullPathFromDefining = definingProjectDirectory != null
                    ? Path.Combine(definingProjectDirectory.Value, item.Name)
                    : item.Name;

                string projectFileFullPath = Path.GetFullPath(fullPathFromDefining);

                ((ReferenceProperties)item.Properties).Add(new ReferenceProperty
                {
                    Name = ProjectFileFullPathProperty, Value = projectFileFullPath
                });
            }
            return referenceItems;
        }        
    }
}
