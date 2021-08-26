// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.Workloads;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Tracks the set of missing workload packs the .NET projects in a solution.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private)]
    internal interface IMissingWorkloadRegistrationService
    {
        void RegisterMissingWorkloads(Guid projectGuid, ProjectConfiguration projectConfiguration, ISet<WorkloadDescriptor> workloadDescriptors);

        void RegisterProjectConfiguration(Guid projectGuid, ProjectConfiguration projectConfiguration);

        void UnregisterProjectConfiguration(Guid projectGuid, ProjectConfiguration projectConfiguration);
    }
}
