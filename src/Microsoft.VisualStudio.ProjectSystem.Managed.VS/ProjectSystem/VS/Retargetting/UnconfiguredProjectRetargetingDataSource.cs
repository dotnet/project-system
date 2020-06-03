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
    internal partial class UnconfiguredProjectRetargetingDataSource : ConfiguredToUnconfiguredChainedDataSourceBase<IImmutableList<ProjectTargetChange>, ProjectTargetChange>, IUnconfiguredProjectRetargetingDataSource, IProjectDynamicLoadComponent
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectRetargetingManager _projectRetargetingManager;

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

        protected override IProjectValueDataSource<IImmutableList<ProjectTargetChange>>? GetInputDataSource(ConfiguredProject configuredProject)
        {
            return configuredProject.Services.ExportProvider.GetExportedValueOrDefault<IConfiguredProjectRetargetingDataSource>();
        }

        protected override ProjectTargetChange ConvertInputData(IReadOnlyCollection<IImmutableList<ProjectTargetChange>> inputs)
        {
            // Remove duplicates so that we have one target change per project
            ImmutableList<ProjectTargetChange> changes = ImmutableList<ProjectTargetChange>.Empty
                                                                                   .AddRange(inputs.SelectMany(projects => projects)
                                                                                   .Distinct(change => change.NewTargetId, EqualityComparer<Guid>.Default));

            ProjectTargetChange changeToProcess = ProjectTargetChange.None;
            if (changes.Count > 0)
            {
                // TODO: Make this smarter, and select the best retarget to offer
                changeToProcess = changes[0];
            }

            _projectRetargetingManager.ReportProjectNeedsRetargeting(_project.FullPath, changeToProcess);

            return changeToProcess;
        }
    }
}
