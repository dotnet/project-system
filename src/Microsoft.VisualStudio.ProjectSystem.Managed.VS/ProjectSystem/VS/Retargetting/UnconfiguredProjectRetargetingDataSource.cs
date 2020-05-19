// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [Export(typeof(IUnconfiguredProjectRetargetingDataSource))]
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class UnconfiguredProjectRetargetingDataSource : ConfiguredToUnconfiguredChainedDataSourceBase<IImmutableList<TargetDescriptionBase>, IImmutableList<IProjectTargetChange>>, IUnconfiguredProjectRetargetingDataSource, IProjectDynamicLoadComponent
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectRetargetingManager _projectRetargetingManager;
        private readonly IVsService<IVsTrackProjectRetargeting2> _retargettingService;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        internal UnconfiguredProjectRetargetingDataSource(UnconfiguredProject project,
                                                          IProjectRetargetingManager projectRetargetingManager,
                                                          IActiveConfigurationGroupService activeConfigurationGroupService,
                                                          IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> retargettingService,
                                                          IProjectThreadingService threadingService)
             : base(project, activeConfigurationGroupService)
        {
            _project = project;
            _projectRetargetingManager = projectRetargetingManager;
            _retargettingService = retargettingService;
            _threadingService = threadingService;
        }


        public Task LoadAsync()
        {
            EnsureInitialized();

            _projectRetargetingManager.ReportProjectMightRetarget(_project.FullPath);

            return Task.CompletedTask;
        }

        public Task UnloadAsync()
        {
            return Task.CompletedTask;
        }

        protected override IProjectValueDataSource<IImmutableList<TargetDescriptionBase>>? GetInputDataSource(ConfiguredProject configuredProject)
        {
            return configuredProject.Services.ExportProvider.GetExportedValueOrDefault<IConfiguredProjectRetargetingDataSource>();
        }

        protected override IImmutableList<IProjectTargetChange> ConvertInputData(IReadOnlyCollection<IImmutableList<TargetDescriptionBase>> inputs)
        {
            // For now, we just remove duplicates so that we have one target change per project
            var changes = ImmutableList<IProjectTargetChange>.Empty.AddRange(inputs.SelectMany(projects => projects)
                                                                                   .Select(changes => changes)
                                                                                   .Distinct(change => change.TargetId, EqualityComparer<Guid>.Default)
                                                                                   .Select(c=>new ProjectTargetChange(c)));

            _threadingService.ExecuteSynchronously(async () =>
             {
                 IVsTrackProjectRetargeting2 trackProjectRetageting = await _retargettingService.GetValueAsync();

                 foreach (ProjectTargetChange change in changes)
                 {
                     trackProjectRetageting.RegisterProjectTarget(change.Description);
                 }
             });

            _projectRetargetingManager.ReportProjectNeedsRetargeting(_project.FullPath, changes.Any());

            return changes;
        }

        internal class ProjectTargetChange : IProjectTargetChange
        {
            private readonly TargetDescriptionBase _targetDescription;

            public ProjectTargetChange(TargetDescriptionBase targetDescription)
            {
                _targetDescription = targetDescription;
            }

            public TargetDescriptionBase Description => _targetDescription;

            public Guid NewTargetId => _targetDescription.TargetId;

            public Guid CurrentTargetId => Guid.Empty;

            public bool ReloadProjectOnSuccess => true;

            public bool UnloadOnFailure => true;

            public bool UnloadOnCancel => true;
        }
    }
}
