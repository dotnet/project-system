// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.ProjectSystem.Workloads;
using Microsoft.VisualStudio.RpcContracts.Setup;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Tracks the set of missing workload packs for each .NET project in a solution.
    /// </summary>
    [Export(typeof(IMissingWorkloadRegistrationService))]
    internal class MissingWorkloadRegistrationService : OnceInitializedOnceDisposedAsync, IMissingWorkloadRegistrationService
    {
        private readonly IDictionary<Guid, ISet<WorkloadDescriptor>> _projectGuidToWorkloadDescriptorsMap;
        private readonly IDictionary<Guid, ISet<ProjectConfiguration>> _projectGuidToProjectConfigurationsMap;
        private readonly IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> _serviceBrokerContainer;
        private readonly IVisualStudioComponentIdTransformer _visualStudioComponentIdTransformer;

        [ImportingConstructor]
        public MissingWorkloadRegistrationService(
            JoinableTaskContext joinableTaskContext,
            IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> serviceBrokerContainer,
            IProjectThreadingService projectThreadingService,
            IVisualStudioComponentIdTransformer visualStudioComponentIdTransformer)
            : base(new JoinableTaskContextNode(joinableTaskContext))
        {
            _projectGuidToWorkloadDescriptorsMap = new Dictionary<Guid, ISet<WorkloadDescriptor>>();
            _projectGuidToProjectConfigurationsMap = new Dictionary<Guid, ISet<ProjectConfiguration>>();
            _serviceBrokerContainer = serviceBrokerContainer;
            _visualStudioComponentIdTransformer = visualStudioComponentIdTransformer;
        }

        public void ClearMissingWorkloadMetadata()
        {
            _projectGuidToWorkloadDescriptorsMap.Clear();
        }

        public Task RegisterMissingWorkloadAsync(Guid projectGuid, ProjectConfiguration projectConfiguration, WorkloadDescriptor workloadDescriptor, CancellationToken cancellationToken)
        {
            if (!WorkloadDescriptor.Empty.Equals(workloadDescriptor))
            {
                if (!_projectGuidToWorkloadDescriptorsMap.TryGetValue(projectGuid, out var workloadDescriptorSet))
                {
                    workloadDescriptorSet = new HashSet<WorkloadDescriptor>();
                    _projectGuidToWorkloadDescriptorsMap[projectGuid] = workloadDescriptorSet;
                }

                workloadDescriptorSet.Add(workloadDescriptor);
            }

            if (_projectGuidToProjectConfigurationsMap.TryGetValue(projectGuid, out var projectConfigurationSet))
            {
                projectConfigurationSet.Remove(projectConfiguration);
                if (projectConfigurationSet.Count == 0 && ShouldDisplayMissingComponentsPrompt())
                {
                    return DisplayMissingComponentsPromptAsync(cancellationToken);
                }
            }

            return Task.CompletedTask;
        }

        public void RegisterProjectConfiguration(Guid projectGuid, ProjectConfiguration projectConfiguration)
        {
            if (!_projectGuidToProjectConfigurationsMap.TryGetValue(projectGuid, out var projectConfigurationSet))
            {
                projectConfigurationSet = new HashSet<ProjectConfiguration>();
                _projectGuidToProjectConfigurationsMap[projectGuid] = projectConfigurationSet;
            }

            projectConfigurationSet.Add(projectConfiguration);
        }

        private bool ShouldDisplayMissingComponentsPrompt()
        {
            return _projectGuidToProjectConfigurationsMap.Values.All(projectConfigurationSet => projectConfigurationSet.Count == 0);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            ClearMissingWorkloadMetadata();

            return Task.CompletedTask;
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task DisplayMissingComponentsPromptAsync(CancellationToken cancellationToken)
        {
            if (_projectGuidToWorkloadDescriptorsMap.Count == 0)
            {
                return;
            }

            var serviceBrokerContainer = await _serviceBrokerContainer.GetValueAsync();
            IServiceBroker? serviceBroker = serviceBrokerContainer?.GetFullAccessServiceBroker();
            if (serviceBroker == null)
            {
                return;
            }

            var missingWorkloadRegistrationService = await serviceBroker.GetProxyAsync<IMissingComponentRegistrationService>(
                serviceDescriptor: VisualStudioServices.VS2022.MissingComponentRegistrationService);

            using (missingWorkloadRegistrationService as IDisposable)
            {
                if (missingWorkloadRegistrationService != null)
                {
                    foreach (var (projectGuid, vsComponents) in _projectGuidToWorkloadDescriptorsMap)
                    {
                        var vsComponentIds = vsComponents.Select(workloadDescriptor => workloadDescriptor.VisualStudioComponentId);
                        var transformedVsComponentIds = await _visualStudioComponentIdTransformer.TransformVisualStudioComponentIdsAsync(vsComponentIds.ToArray());

                        await missingWorkloadRegistrationService.RegisterMissingComponentsAsync(projectGuid, transformedVsComponentIds.ToArray(), cancellationToken);
                    }

                    await missingWorkloadRegistrationService.DisplayMissingComponentsPromptAsync(cancellationToken);
                }
            }
        }
    }
}
