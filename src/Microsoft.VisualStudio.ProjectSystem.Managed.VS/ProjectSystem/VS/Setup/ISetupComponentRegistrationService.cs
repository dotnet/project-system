// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup
{
    /// <summary>
    /// Aggregates the set of components required by projects across the solution.
    /// When required components are absent, triggers the in-product acquisition to allow the user to easily launch the installer.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private)]
    internal interface ISetupComponentRegistrationService
    {
        /// <summary>
        /// Integrates a set of suggested workloads for the specified project configuration.
        /// </summary>
        /// <remarks>
        /// Must register a project first using <see cref="RegisterProjectConfiguration" />
        /// </remarks>
        void SetSuggestedWorkloads(Guid projectGuid, ConfiguredProject project, ISet<WorkloadDescriptor> workloadDescriptors);

        /// <summary>
        /// Integrates a set of suggested web workloads for the specified project configuration.
        /// </summary>
        /// <remarks>
        /// Must register a project first using <see cref="RegisterProjectConfiguration" />
        /// </remarks>
        void SetSuggestedWebWorkloads(Guid projectGuid, ConfiguredProject project, ISet<WorkloadDescriptor> workloadDescriptors);

        /// <summary>
        /// Sets the .NET Core runtime version (e.g. <c>v6.0</c>) required by the project.
        /// </summary>
        /// <remarks>
        /// Must register a project first using <see cref="RegisterProjectConfiguration" />
        /// </remarks>
        void SetRuntimeVersion(Guid projectGuid, ConfiguredProject project, string runtimeVersion);

        /// <summary>
        /// Register a project to be tracked for components to be installed.
        /// </summary>
        /// <remarks>
        /// This service must be initialized first.
        /// </remarks>
        /// <returns>An <see cref="IDisposable"/> that unregisters the project configuration when disposed.</returns>
        IDisposable RegisterProjectConfiguration(Guid projectGuid, ConfiguredProject project);
    }
}
