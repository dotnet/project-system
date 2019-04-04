// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Properties;

using NuGet.SolutionRestoreManager;

using RestoreUpdate = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<Microsoft.VisualStudio.ProjectSystem.VS.NuGet.ProjectRestoreUpdate>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Provides an implementation of <see cref="IPackageRestoreConfiguredDataSource"/> that combines evaluations results
    ///     of <see cref="DotNetCliToolReference"/>, <see cref="ProjectReference"/> and project build versions of <see cref="PackageReference"/> 
    ///     into <see cref="ProjectRestoreUpdate"/>.
    /// </summary>
    [Export(typeof(IPackageRestoreConfiguredDataSource))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class PackageRestoreConfiguredDataSource : ChainedProjectValueDataSourceBase<ProjectRestoreUpdate>, IPackageRestoreConfiguredDataSource
    {
        private static readonly ImmutableHashSet<string> s_rules = Empty.OrdinalIgnoreCaseStringSet
                                                                        .Add(NuGetRestore.SchemaName)               // Evaluation
                                                                        .Add(ProjectReference.SchemaName)           // Evaluation
                                                                        .Add(PackageReference.SchemaName)           // Project Build
                                                                        .Add(DotNetCliToolReference.SchemaName);    // Evaluation

        private readonly UnconfiguredProject _project;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        public PackageRestoreConfiguredDataSource(ConfiguredProject project, IProjectSubscriptionService projectSubscriptionService)
            : base(project.Services, synchronousDisposal: true, registerDataSource: false)
        {
            _project = project.UnconfiguredProject;
            _projectSubscriptionService = projectSubscriptionService;
        }

        protected override UnconfiguredProject ContainingProject
        {
            get { return _project; }
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<RestoreUpdate> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.JointRuleSource;

            // Transform the changes from evaluation/design-time build -> restore data
            DisposableValue<ISourceBlock<RestoreUpdate>> transformBlock = source.SourceBlock
                                                                                .TransformWithNoDelta(update => update.Derive(u => CreateRestoreUpdate(u.ProjectConfiguration, u.CurrentState)),
                                                                                                      ruleNames: s_rules);

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete 
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private ProjectRestoreUpdate CreateRestoreUpdate(ProjectConfiguration projectConfiguration, IImmutableDictionary<string, IProjectRuleSnapshot> update)
        {
            IVsProjectRestoreInfo restoreInfo = RestoreBuilder.ToProjectRestoreInfo(update);

            return new ProjectRestoreUpdate(projectConfiguration, restoreInfo);
        }
    }
}
