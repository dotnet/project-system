// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Runtimes;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class MissingSdkRuntimeDetector : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
    {
        private Guid _projectGuid;
        private bool _enabled;
        private bool? _hasNoMissingSdkRuntimes;
        private IDisposable? _joinedDataSources;
        private IDisposable? _subscription;

        private readonly ConfiguredProject _project;
        private readonly IRuntimeDescriptorDataSource _runtimeDescriptorDataSource;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;
        private readonly IProjectSubscriptionService _projectSubscriptionService;
        private readonly IMissingSetupComponentRegistrationService _missingSetupComponentRegistrationService;

        [ImportingConstructor]
        public MissingSdkRuntimeDetector(
            IMissingSetupComponentRegistrationService missingSetupComponentRegistrationService,
            ConfiguredProject configuredProject,
            IRuntimeDescriptorDataSource runtimeDescriptorDataSource,
            IProjectSubscriptionService projectSubscriptionService,
            IProjectFaultHandlerService projectFaultHandlerService,
            IProjectThreadingService threadingService)
            : base(threadingService.JoinableTaskContext)
        {
            _missingSetupComponentRegistrationService = missingSetupComponentRegistrationService;
            _project = configuredProject;
            _runtimeDescriptorDataSource = runtimeDescriptorDataSource;
            _projectSubscriptionService = projectSubscriptionService;
            _projectFaultHandlerService = projectFaultHandlerService;
        }

        public Task LoadAsync()
        {
            _enabled = true;
            return InitializeAsync();
        }

        public Task UnloadAsync()
        {
            _enabled = false;

            _missingSetupComponentRegistrationService.UnregisterProjectConfiguration(_projectGuid, _project);

            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            return Task.CompletedTask;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _projectGuid = await _project.UnconfiguredProject.GetProjectGuidAsync();
            _joinedDataSources = ProjectDataSources.JoinUpstreamDataSources(JoinableFactory, _projectFaultHandlerService, _projectSubscriptionService.ProjectSource, _runtimeDescriptorDataSource);

            _missingSetupComponentRegistrationService.RegisterProjectConfiguration(_projectGuid, _project);

            Action<IProjectVersionedValue<ValueTuple<IProjectSnapshot, ISet<RuntimeDescriptor>>>> action = OnRuntimeDescriptorsComputed;

            _subscription = ProjectDataSources.SyncLinkTo(
                _projectSubscriptionService.ProjectSource.SourceBlock.SyncLinkOptions(),
                _runtimeDescriptorDataSource.SourceBlock.SyncLinkOptions(),
                    DataflowBlockFactory.CreateActionBlock(action, _project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality),
                    linkOptions: DataflowOption.PropagateCompletion,
                    cancellationToken: cancellationToken);
        }

        private void OnRuntimeDescriptorsComputed(IProjectVersionedValue<(IProjectSnapshot projectSnapshot, ISet<RuntimeDescriptor> runtimeDescriptors)> pair)
        {
            if (!_enabled || _hasNoMissingSdkRuntimes == true)
            {
                return;
            }

            if (pair.Value.runtimeDescriptors.Count == 0)
            {
                _hasNoMissingSdkRuntimes = true;
            }
            else
            {
                var runtimeDescriptor = pair.Value.runtimeDescriptors.FirstOrDefault();
                _missingSetupComponentRegistrationService.RegisterMissingSdkRuntimes(_projectGuid, _project, runtimeDescriptor);
            }
        }
    }
}
