// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.Workloads;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Tracks the set of missing .NET workloads for a configured project.
    /// </summary>
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class MissingWorkloadDetector : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
    {
        private readonly ConfiguredProject _project;
        private readonly IMissingWorkloadRegistrationService _missingWorkloadRegistrationService;
        private readonly IWorkloadDescriptorDataSource _workloadDescriptorDataSource;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        private bool _enabled;
        private Guid _projectGuid;

        private IDisposable? _joinedDataSources;
        private IDisposable? _subscription;
        private bool? _hasNoMissingWorkloads;
        private ISet<WorkloadDescriptor>? _missingWorkloads;

        [ImportingConstructor]
        public MissingWorkloadDetector(
            ConfiguredProject project,
            IWorkloadDescriptorDataSource workloadDescriptorDataSource,
            IMissingWorkloadRegistrationService missingWorkloadRegistrationService,
            IProjectThreadingService threadingService,
            IProjectFaultHandlerService projectFaultHandlerService,
            IProjectSubscriptionService projectSubscriptionService)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _workloadDescriptorDataSource = workloadDescriptorDataSource;
            _missingWorkloadRegistrationService = missingWorkloadRegistrationService;
            _projectFaultHandlerService = projectFaultHandlerService;
            _projectSubscriptionService = projectSubscriptionService;
        }

        public Task LoadAsync()
        {
            _enabled = true;

            return InitializeAsync();
        }

        public Task UnloadAsync()
        {
            _enabled = false;

            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _subscription?.Dispose();
            _joinedDataSources?.Dispose();

            return Task.CompletedTask;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _projectGuid = await _project.UnconfiguredProject.GetProjectGuidAsync();
            _joinedDataSources = ProjectDataSources.JoinUpstreamDataSources(JoinableFactory, _projectFaultHandlerService, _projectSubscriptionService.ProjectSource, _workloadDescriptorDataSource);

            Action<IProjectVersionedValue<ValueTuple<IProjectSnapshot, ISet<WorkloadDescriptor>>>> action = OnWorkloadDescriptorsComputed;

            _subscription = ProjectDataSources.SyncLinkTo(
                _projectSubscriptionService.ProjectSource.SourceBlock.SyncLinkOptions(),
                _workloadDescriptorDataSource.SourceBlock.SyncLinkOptions(),
                    DataflowBlockFactory.CreateActionBlock(action, _project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality),
                    linkOptions: DataflowOption.PropagateCompletion,
                    cancellationToken: cancellationToken);

            _missingWorkloadRegistrationService.RegisterProjectConfiguration(_projectGuid, _project.ProjectConfiguration);
        }

        private void OnWorkloadDescriptorsComputed(IProjectVersionedValue<(IProjectSnapshot projectSnapshot, ISet<WorkloadDescriptor> workloadDescriptors)> pair)
        {
            if (!_enabled || _hasNoMissingWorkloads == true)
            {
                return;
            }

            if (pair.Value.workloadDescriptors.Count == 0)
            {
                _hasNoMissingWorkloads = true;
            }
            else
            {
                _missingWorkloads ??= new HashSet<WorkloadDescriptor>();
                if (!_missingWorkloads.AddRange(pair.Value.workloadDescriptors))
                {
                    return;
                }
            }

            var task = _missingWorkloadRegistrationService.RegisterMissingWorkloadAsync(_projectGuid, _project.ProjectConfiguration, pair.Value.workloadDescriptors, cancellationToken: default);

            _projectFaultHandlerService.Forget(task, _project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality);
        }
    }
}
