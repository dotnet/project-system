// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.RpcContracts.Setup;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Acquisition
{
    /// <summary>
    /// Breaks loading of projects that require .NET workloads that are not installed.
    /// </summary>
    [Export(typeof(IVetoProjectLoad))]
    [AppliesTo(ProjectCapabilities.Cps)]
    internal class OptionalWorkloadProjectLoadVeto : IVetoProjectLoad
    {
        private const string SuggestedWorkloadItemType = "SuggestedWorkload";
        private const string VisualStudioComponentIdMetadata = "VisualStudioComponentId";

        private readonly IProjectAccessor _projectAccessor;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> _serviceBrokerContainer;

        [ImportingConstructor]
        public OptionalWorkloadProjectLoadVeto(IProjectAccessor projectAccessor,
            UnconfiguredProject unconfiguredProject,
            IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> serviceBrokerContainer)
        {
            _projectAccessor = projectAccessor;
            _unconfiguredProject = unconfiguredProject;
            _serviceBrokerContainer = serviceBrokerContainer;
        }

        /// <summary>
        /// Checks whether the project needs optional workloads (that are not installed)
        /// using the evaluation model. The purpose of this check is to reject loads
        /// of such projects based on the evaluation of the initial project configuration.
        /// </summary>
        async Task<bool> IVetoProjectLoad.AllowProjectLoadAsync(bool isNewProject, CancellationToken cancellationToken)
        {
            ConfiguredProject? configuredProject = await _unconfiguredProject.GetSuggestedConfiguredProjectAsync();
            if (configuredProject == null)
            {
                return true;
            }

            var serviceBrokerContainer = await _serviceBrokerContainer.GetValueAsync(cancellationToken);
            IServiceBroker? serviceBroker = serviceBrokerContainer?.GetFullAccessServiceBroker();
            if (serviceBroker == null)
            {
                return true;
            }

            var missingWorkloadRegistrationService = await serviceBroker.GetProxyAsync<IMissingWorkloadRegistrationService>(
                serviceDescriptor: VisualStudioServices.VS2022.MissingWorkloadRegistrationService,
                cancellationToken: cancellationToken);

            using (missingWorkloadRegistrationService as IDisposable)
            {
                if (missingWorkloadRegistrationService == null)
                {
                    return true;
                }

                var projectGuid = await _unconfiguredProject.GetProjectGuidAsync();

                try
                {
                    return await _projectAccessor.OpenProjectForReadAsync(configuredProject, evaluatedProject =>
                    {
                        var missingWorkloadComponentIds = GetVSComponentIdsForMissingWorkloads(evaluatedProject);

                        if (missingWorkloadComponentIds.Any())
                        {
                            missingWorkloadRegistrationService.RegisterMissingWorkloadComponentIds(projectGuid, missingWorkloadComponentIds);
                            return false;
                        }

                        return true;
                    },
                    cancellationToken);
                }
                finally
                {
                    (missingWorkloadRegistrationService as IDisposable)?.Dispose();
                }
            }
        }

        private IEnumerable<string> GetVSComponentIdsForMissingWorkloads(Project project)
        {
            Requires.NotNull(project, nameof(project));

            var suggestedWorkloadsItems = project.GetItems(SuggestedWorkloadItemType);
            return suggestedWorkloadsItems
                .Where(suggestedWorkloadItem => suggestedWorkloadItem.HasMetadata(VisualStudioComponentIdMetadata))
                .Select(suggestedWorkloadItem => suggestedWorkloadItem.GetMetadataValue(VisualStudioComponentIdMetadata))
                .ToArray();
        }
    }
}
