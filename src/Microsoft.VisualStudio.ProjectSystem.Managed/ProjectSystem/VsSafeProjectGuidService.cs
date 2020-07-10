// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     An implementation of <see cref="ISafeProjectGuidService"/> that waits until the project
    ///     has been loaded into the host environment before returning the project GUID.
    /// </summary>
    [Export(typeof(ISafeProjectGuidService))]
    internal class VsSafeProjectGuidService : ISafeProjectGuidService
    {
        private readonly IUnconfiguredProjectTasksService _tasksService;

        [ImportingConstructor]
        public VsSafeProjectGuidService(UnconfiguredProject project, IUnconfiguredProjectTasksService tasksService)
        {
            _tasksService = tasksService;

#pragma warning disable RS0030 // IProjectGuidService is banned
            ProjectGuidServices = new OrderPrecedenceImportCollection<IProjectGuidService>(projectCapabilityCheckProvider: project);
#pragma warning restore RS0030
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectGuidService> ProjectGuidServices { get; }

        public async Task<Guid> GetProjectGuidAsync()
        {
            await _tasksService.PrioritizedProjectLoadedInHost;

#pragma warning disable RS0030 // IProjectGuidService is banned
            IProjectGuidService? projectGuidService = ProjectGuidServices.FirstOrDefault()?.Value;
            if (projectGuidService == null)
                return Guid.Empty;

            if (projectGuidService is IProjectGuidService2 projectGuidService2)
            {
                return await projectGuidService2.GetProjectGuidAsync();
            }

            return projectGuidService.ProjectGuid;
#pragma warning restore RS0030
        }
    }
}
