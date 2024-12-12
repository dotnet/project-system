﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using RestoreInfo = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<Microsoft.VisualStudio.ProjectSystem.PackageRestore.PackageRestoreUnconfiguredInput>;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

[Export(typeof(IPackageRestoreUnconfiguredInputDataSource))]
[AppliesTo(ProjectCapability.PackageReferences)]
[method: ImportingConstructor]
internal class PackageRestoreUnconfiguredInputDataSource(
    UnconfiguredProject project,
    IActiveConfiguredProjectProvider activeConfiguredProjectProvider,
    IActiveConfigurationGroupService activeConfigurationGroupService)
    : ChainedProjectValueDataSourceBase<PackageRestoreUnconfiguredInput>(
        project,
        synchronousDisposal: false,
        registerDataSource: false),
    IPackageRestoreUnconfiguredInputDataSource
{
    protected override IDisposable LinkExternalInput(ITargetBlock<RestoreInfo> targetBlock)
    {
        // At a high-level, we want to combine all implicitly active configurations (ie the active config of each TFM) restore data
        // (via ProjectRestoreUpdate) and combine it into a single ProjectRestoreInfo instance and publish that. When a change is 
        // made to a configuration, such as adding a PackageReference, we should react to it and push a new version of our output. If the 
        // active configuration changes, we should react to it, and publish data from the new set of implicitly active configurations.

        // Merge across configurations
        var joinBlock = new ConfiguredProjectDataSourceJoinBlock<PackageRestoreConfiguredInput>(
            project => project.Services.ExportProvider.GetExportedValueOrDefault<IPackageRestoreConfiguredInputDataSource>(),
            JoinableFactory,
            ContainingProject!);

        // Transform all restore data -> combined restore data
        IPropagatorBlock<IProjectVersionedValue<(IReadOnlyList<PackageRestoreConfiguredInput>, ConfiguredProject)>, RestoreInfo> transformBlock =
            DataflowBlockSlim.CreateTransformBlock<IProjectVersionedValue<(IReadOnlyList<PackageRestoreConfiguredInput>, ConfiguredProject)>, RestoreInfo>(
                transformFunction: update => update.Derive(MergeRestoreInputs));

        // Sync link in the active configuration
        IDisposable syncLink = ProjectDataSources.SyncLinkTo(
            joinBlock.SyncLinkOptions(),
            activeConfiguredProjectProvider.SourceBlock.SyncLinkOptions(),
            target: transformBlock,
            linkOptions: DataflowOption.PropagateCompletion);

        JoinUpstreamDataSources(activeConfigurationGroupService.ActiveConfiguredProjectGroupSource, activeConfiguredProjectProvider);

        // Set the link up so that we publish changes to target block
        transformBlock.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

        return new DisposableBag
        {
            joinBlock,
            syncLink,

            // Link the active configured projects to our join block
            activeConfigurationGroupService.ActiveConfiguredProjectGroupSource.SourceBlock.LinkTo(joinBlock, DataflowOption.PropagateCompletion),
        };
    }

    private PackageRestoreUnconfiguredInput MergeRestoreInputs((IReadOnlyList<PackageRestoreConfiguredInput> Inputs, ConfiguredProject ActiveConfiguredProject) data)
    {
        (IReadOnlyList<PackageRestoreConfiguredInput> inputs, ConfiguredProject activeConfiguredProject) = data;

        // If there are no updates, we have no active configurations
        ProjectRestoreInfo? restoreInfo = null;

        if (inputs.Count is not 0)
        {
            // We need to combine the snapshots from each implicitly active configuration (ie per TFM), 
            // resolving any conflicts, which we'll report to the user.
            string msBuildProjectExtensionsPath = ResolveMSBuildProjectExtensionsPathConflicts(inputs);
            string originalTargetFrameworks = ResolveOriginalTargetFrameworksConflicts(inputs);
            string projectAssetsFilePath = ResolveProjectAssetsFilePathConflicts(inputs);
            ImmutableArray<ReferenceItem> toolReferences = ResolveToolReferenceConflicts(inputs);
            ImmutableArray<TargetFrameworkInfo> targetFrameworks = GetAllTargetFrameworks(inputs);

            restoreInfo = new ProjectRestoreInfo(
                msBuildProjectExtensionsPath,
                projectAssetsFilePath,
                originalTargetFrameworks,
                targetFrameworks,
                toolReferences);
        }

        return new PackageRestoreUnconfiguredInput(restoreInfo, inputs, activeConfiguredProject.ProjectConfiguration);
    }

    private string ResolveProjectAssetsFilePathConflicts(IReadOnlyCollection<PackageRestoreConfiguredInput> updates)
    {
        // All configurations need to agree on where the project-wide asset file is located.
        return ResolvePropertyConflicts(updates, u => u.ProjectAssetsFilePath, NuGetRestore.ProjectAssetsFileProperty);
    }

    private string ResolveMSBuildProjectExtensionsPathConflicts(IReadOnlyCollection<PackageRestoreConfiguredInput> updates)
    {
        // All configurations need to agree on where the project-wide extensions path is located
        return ResolvePropertyConflicts(updates, u => u.MSBuildProjectExtensionsPath, NuGetRestore.MSBuildProjectExtensionsPathProperty);
    }

    private string ResolveOriginalTargetFrameworksConflicts(IReadOnlyCollection<PackageRestoreConfiguredInput> updates)
    {
        // All configurations need to agree on what the overall "user-written" frameworks for the 
        // project so that conditions in the project-wide 'nuget.g.props' and 'nuget.g.targets' 
        // are written and evaluated correctly.
        return ResolvePropertyConflicts(updates, u => u.OriginalTargetFrameworks, NuGetRestore.TargetFrameworksProperty);
    }

    private string ResolvePropertyConflicts(IReadOnlyCollection<PackageRestoreConfiguredInput> updates, Func<ProjectRestoreInfo, string> propertyGetter, string propertyName)
    {
        // Always use the first TFM listed in project to provide consistent behavior
        PackageRestoreConfiguredInput update = updates.First();
        string propertyValue = propertyGetter(update.RestoreInfo);

        // Every config should had same value
        bool hasConflicts = updates.Select(u => propertyGetter(u.RestoreInfo))
                                   .Distinct(StringComparers.PropertyNames)
                                   .Skip(1)
                                   .Any();

        if (hasConflicts)
        {
            ReportUserFault(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Restore_PropertyWithInconsistentValues,
                    propertyName,
                    propertyValue,
                    update.ProjectConfiguration));
        }

        return propertyValue;
    }

    private ImmutableArray<ReferenceItem> ResolveToolReferenceConflicts(IEnumerable<PackageRestoreConfiguredInput> updates)
    {
        var references = new Dictionary<string, ReferenceItem>(StringComparers.ItemNames);

        foreach (PackageRestoreConfiguredInput update in updates)
        {
            foreach (ReferenceItem reference in update.RestoreInfo.ToolReferences)
            {
                if (ValidateToolReference(references, reference))
                {
                    references.Add(reference.Name, reference);
                }
            }
        }

        return ImmutableArray.CreateRange(references.Values);
    }
    private ImmutableArray<TargetFrameworkInfo> GetAllTargetFrameworks(IEnumerable<PackageRestoreConfiguredInput> updates)
    {
        var frameworks = ImmutableArray.CreateBuilder<TargetFrameworkInfo>();

        foreach (PackageRestoreConfiguredInput update in updates)
        {
            Assumes.True(update.RestoreInfo.TargetFrameworks.Length == 1);

            TargetFrameworkInfo framework = update.RestoreInfo.TargetFrameworks[0];

            if (ValidateTargetFramework(update.ProjectConfiguration, framework))
            {
                frameworks.Add(framework);
            }
        }

        return frameworks.ToImmutable();
    }

    private bool ValidateToolReference(Dictionary<string, ReferenceItem> existingReferences, ReferenceItem reference)
    {
        if (existingReferences.TryGetValue(reference.Name, out ReferenceItem? existingReference))
        {
            // CLI tool references are project-wide, so if they have conflicts in names, 
            // they must have the same metadata, which avoids from having to condition 
            // them so that they only appear in one TFM.
            if (!RestoreComparer.ReferenceItems.Equals(existingReference, reference))
            {
                ReportUserFault(string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Restore_DuplicateToolReferenceItems,
                    existingReference.Name));
            }

            return false;
        }

        return true;
    }

    private bool ValidateTargetFramework(ProjectConfiguration projectConfiguration, TargetFrameworkInfo framework)
    {
        if (framework.TargetFrameworkMoniker.Length == 0)
        {
            ReportUserFault(string.Format(
                CultureInfo.CurrentCulture,
                Resources.Restore_EmptyTargetFrameworkMoniker,
                projectConfiguration.Name));

            return false;
        }

        return true;
    }

    private void ReportUserFault(string message)
    {
        try
        {
            throw new Exception(message);
        }
        catch (Exception ex)
        {
            ReportDataSourceUserFault(
              ex,
              ProjectFaultSeverity.LimitedFunctionality,
              ContainingProject!);
        }
    }
}
