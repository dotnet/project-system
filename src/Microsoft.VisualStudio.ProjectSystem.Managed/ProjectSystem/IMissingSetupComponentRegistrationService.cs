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
        Task InitializeAsync(CancellationToken cancellationToken = default);

        void RegisterMissingWorkloads(Guid projectGuid, ConfiguredProject project, ISet<WorkloadDescriptor> workloadDescriptors);

        void RegisterPossibleMissingSdkRuntimeVersion(Guid projectGuid, ConfiguredProject project, string runtimeVersion);

        void RegisterProjectConfiguration(Guid projectGuid, ConfiguredProject project);

        void UnregisterProjectConfiguration(Guid projectGuid, ConfiguredProject project);
    }
}
