﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using RestoreUpdate = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<Microsoft.VisualStudio.ProjectSystem.PackageRestore.PackageRestoreConfiguredInput>;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

/// <summary>
///     Provides an implementation of <see cref="IPackageRestoreConfiguredInputDataSource"/> that combines
///     evaluation and build data into <see cref="PackageRestoreConfiguredInput"/>.
/// </summary>
[Export(typeof(IPackageRestoreConfiguredInputDataSource))]
[AppliesTo(ProjectCapability.PackageReferences)]
[ExportInitialBuildRulesSubscriptions([
    CollectedFrameworkReference.SchemaName,
    CollectedPackageDownload.SchemaName,
    CollectedPackageVersion.SchemaName,
    CollectedNuGetAuditSuppressions.SchemaName,
    CollectedPrunePackageReference.SchemaName,
    CollectedPackageReference.SchemaName])]
internal class PackageRestoreConfiguredInputDataSource : ChainedProjectValueDataSourceBase<PackageRestoreConfiguredInput>, IPackageRestoreConfiguredInputDataSource
{
    private static readonly ImmutableHashSet<string> s_rules = Empty.OrdinalIgnoreCaseStringSet
        .Add(NuGetRestore.SchemaName)                       // Evaluation
        .Add(EvaluatedProjectReference.SchemaName)          // Evaluation
        .Add(DotNetCliToolReference.SchemaName)             // Evaluation
        .Add(CollectedFrameworkReference.SchemaName)        // Build
        .Add(CollectedPackageDownload.SchemaName)           // Build
        .Add(CollectedPackageVersion.SchemaName)            // Build
        .Add(CollectedNuGetAuditSuppressions.SchemaName)    // Build
        .Add(CollectedPrunePackageReference.SchemaName)     // Build
        .Add(CollectedPackageReference.SchemaName);         // Build

    private readonly IProjectSubscriptionService _projectSubscriptionService;

    [ImportingConstructor]
    public PackageRestoreConfiguredInputDataSource(ConfiguredProject project, IProjectSubscriptionService projectSubscriptionService)
        : base(project, synchronousDisposal: false, registerDataSource: false)
    {
        _projectSubscriptionService = projectSubscriptionService;
    }

    protected override IDisposable LinkExternalInput(ITargetBlock<RestoreUpdate> targetBlock)
    {
        IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.JointRuleSource;

        // Transform the changes from evaluation/design-time build -> restore data
        DisposableValue<ISourceBlock<RestoreUpdate>> transformBlock = source.SourceBlock.TransformWithNoDelta(
            update => update.Derive(u => CreateRestoreInput(update, u.ProjectConfiguration, u.CurrentState)),
            suppressVersionOnlyUpdates: false,    // We need to coordinate these at the unconfigured-level
            ruleNames: s_rules);

        // Set the link up so that we publish changes to target block
        transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

        // Join the source blocks, so if they need to switch to UI thread to complete 
        // and someone is blocked on us on the same thread, the call proceeds
        JoinUpstreamDataSources(source);

        return transformBlock;
    }

    private static PackageRestoreConfiguredInput CreateRestoreInput(IProjectVersionedValue<IProjectSubscriptionUpdate> projectSubscriptionUpdate, ProjectConfiguration projectConfiguration, IImmutableDictionary<string, IProjectRuleSnapshot> update)
    {
        var restoreInfo = RestoreBuilder.ToProjectRestoreInfo(update);

        IComparable configuredProjectVersion = projectSubscriptionUpdate.DataSourceVersions[ProjectDataSources.ConfiguredProjectVersion];

        return new PackageRestoreConfiguredInput(projectConfiguration, restoreInfo, configuredProjectVersion);
    }
}
