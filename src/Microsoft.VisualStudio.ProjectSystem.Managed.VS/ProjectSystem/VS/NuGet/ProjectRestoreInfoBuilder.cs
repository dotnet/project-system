// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Utilities;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal static class ProjectRestoreInfoBuilder
    {
        internal static IVsProjectRestoreInfo Build(IEnumerable<IProjectVersionedValue<IProjectSubscriptionUpdate>> updates)
        {
            Requires.NotNull(updates, nameof(updates));

            // if none of the underlying subscriptions have any changes
            if (!updates.Any(u => u.Value.ProjectChanges.Any(c => c.Value.Difference.AnyChanges)))
            {
                return null;
            }

            string msbuildProjectExtensionsPath = null;
            string originalTargetFrameworks = null;
            var targetFrameworks = ImmutableDictionary.Create<string, IVsTargetFrameworkInfo>(StringComparers.ItemNames);
            var toolReferences = ImmutableDictionary.Create<string, IVsReferenceItem>(StringComparers.ItemNames);

            foreach (IProjectVersionedValue<IProjectSubscriptionUpdate> update in updates)
            {
                IProjectChangeDescription nugetRestoreChanges = update.Value.ProjectChanges[NuGetRestore.SchemaName];
                msbuildProjectExtensionsPath = msbuildProjectExtensionsPath ??
                    nugetRestoreChanges.After.Properties[NuGetRestore.MSBuildProjectExtensionsPathProperty];
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

                if (!targetFrameworks.ContainsKey(targetFramework))
                {
                    IProjectChangeDescription projectReferencesChanges = update.Value.ProjectChanges[ProjectReference.SchemaName];
                    IProjectChangeDescription packageReferencesChanges = update.Value.ProjectChanges[PackageReference.SchemaName];

                    targetFrameworks = targetFrameworks.Add(targetFramework, new TargetFrameworkInfo(
                        targetFramework,
                        RestoreBuilder.ToReferenceItems(projectReferencesChanges.After.Items),
                        RestoreBuilder.ToReferenceItems(packageReferencesChanges.After.Items),
                        RestoreBuilder.ToProjectProperties(nugetRestoreChanges.After.Properties)
                    ));
                }

                IProjectChangeDescription toolReferencesChanges = update.Value.ProjectChanges[DotNetCliToolReference.SchemaName];
                foreach (KeyValuePair<string, IImmutableDictionary<string, string>> item in toolReferencesChanges.After.Items)
                {
                    if (!toolReferences.ContainsKey(item.Key))
                    {
                        toolReferences = toolReferences.Add(item.Key, RestoreBuilder.ToReferenceItem(item.Key, item.Value));
                    }
                }
            }

            // return nominate restore information if any target framework entries are found
            return targetFrameworks.Count > 0
                ? new ProjectRestoreInfo(
                    // NOTE: We pass MSBuildProjectExtensionsPath as BaseIntermediatePath instead of using
                    // BaseIntermediateOutputPath. This is because NuGet switched from using BaseIntermediateOutputPath
                    // to MSBuildProjectExtensionsPath, since the value of BaseIntermediateOutputPath is often set too
                    // late (after *.g.props files would need to have been imported from it). Instead of modifying the
                    // IVsProjectRestoreInfo interface or introducing something like IVsProjectRestoreInfo with an
                    // MSBuildProjectExtensionsPath property, we opted to leave the interface the same but change the
                    // meaning of its BaseIntermediatePath property. See
                    // https://github.com/dotnet/project-system/issues/3466for for details.
                    msbuildProjectExtensionsPath,
                    originalTargetFrameworks,
                    new TargetFrameworks(targetFrameworks.Values),
                    new ReferenceItems(toolReferences.Values)
                )
                : null;
        }
    }
}
