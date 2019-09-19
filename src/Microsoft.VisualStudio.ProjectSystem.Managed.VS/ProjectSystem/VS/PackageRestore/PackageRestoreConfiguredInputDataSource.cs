// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using RestoreUpdate = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore.PackageRestoreConfiguredInput>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Provides an implementation of <see cref="IPackageRestoreConfiguredInputDataSource"/> that combines evaluations results
    ///     of <see cref="DotNetCliToolReference"/>, <see cref="ProjectReference"/> and project build versions of <see cref="PackageReference"/> 
    ///     into <see cref="PackageRestoreConfiguredInput"/>.
    /// </summary>
    [Export(typeof(IPackageRestoreConfiguredInputDataSource))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class PackageRestoreConfiguredInputDataSource : ChainedProjectValueDataSourceBase<PackageRestoreConfiguredInput>, IPackageRestoreConfiguredInputDataSource
    {
        private static readonly ImmutableHashSet<string> s_rules = Empty.OrdinalIgnoreCaseStringSet
                                                                        .Add(NuGetRestore.SchemaName)                       // Evaluation
                                                                        .Add(ProjectReference.SchemaName)                   // Evaluation
                                                                        .Add(DotNetCliToolReference.SchemaName)             // Evaluation
                                                                        .Add(CollectedFrameworkReference.SchemaName)        // Project Build
                                                                        .Add(CollectedPackageDownload.SchemaName)           // Project Build                                                                        
                                                                        .Add(PackageReference.SchemaName);                  // Project Build
        private readonly UnconfiguredProject _containingProject;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        public PackageRestoreConfiguredInputDataSource(ConfiguredProject project, IProjectSubscriptionService projectSubscriptionService)
            : base(project.Services, synchronousDisposal: true, registerDataSource: false)
        {
            _containingProject = project.UnconfiguredProject;
            _projectSubscriptionService = projectSubscriptionService;
        }

        protected override UnconfiguredProject ContainingProject
        {
            get { return _containingProject; }
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<RestoreUpdate> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.JointRuleSource;

            // Transform the changes from evaluation/design-time build -> restore data
            DisposableValue<ISourceBlock<RestoreUpdate>> transformBlock = source.SourceBlock.TransformWithNoDelta(update => update.Derive(u => CreateRestoreInput(u.ProjectConfiguration, u.CurrentState)),
                                                                                                suppressVersionOnlyUpdates: false,    // We need to coordinate these at the unconfigured-level
                                                                                                ruleNames: s_rules);

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete 
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private static PackageRestoreConfiguredInput CreateRestoreInput(ProjectConfiguration projectConfiguration, IImmutableDictionary<string, IProjectRuleSnapshot> update)
        {
            var restoreInfo = RestoreBuilder.ToProjectRestoreInfo(update);

            return new PackageRestoreConfiguredInput(projectConfiguration, restoreInfo);
        }
    }
}
