// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Workloads;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Tracks the set of missing workload packs and SDK runtimes the .NET projects in a solution
    ///     need to improve the development experience.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private)]
    internal interface IMissingSetupComponentRegistrationService
    {
        /// <summary>
        /// Initilize this service.
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Register per project a set of component ids needed for the project to work correctly.
        /// The components will be installed if they are not in the IDE.
        /// </summary>
        /// <remarks>
        /// Must register a project first using <see cref="RegisterProjectConfiguration" />
        /// </remarks>
        void RegisterMissingWorkloads(Guid projectGuid, ConfiguredProject project, ISet<WorkloadDescriptor> workloadDescriptors);

        /// <summary>
        /// Register per project component ids of Web projects needed for the project to work correctly.
        /// The components will be installed if they not in the IDE.
        /// </summary>
        void RegisterMissingWebWorkloads(Guid projectGuid, ConfiguredProject project, ISet<WorkloadDescriptor> workloadDescriptors);

        /// <summary>
        /// Register per project the version of sdk net core runtime (e.g. v6.0) to be installed if it is missing.
        /// </summary>
        /// <remarks>
        /// Must register a project first using <see cref="RegisterProjectConfiguration" />
        /// </remarks>
        void RegisterPossibleMissingSdkRuntimeVersion(Guid projectGuid, ConfiguredProject project, string runtimeVersion);

        /// <summary>
        /// Register a project to be tracked for components to be installed.
        /// </summary>
        /// <remarks>
        /// This service must be initilized first.
        /// </remarks>
        void RegisterProjectConfiguration(Guid projectGuid, ConfiguredProject project);

        /// <summary>
        /// Unregister a project if no longer tracked.
        /// </summary>
        void UnregisterProjectConfiguration(Guid projectGuid, ConfiguredProject project);
    }
}
