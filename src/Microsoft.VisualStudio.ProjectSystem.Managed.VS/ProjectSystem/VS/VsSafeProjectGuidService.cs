// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IProjectAsyncLoadDashboard _loadDashboard;

        [ImportingConstructor]
        public VsSafeProjectGuidService(UnconfiguredProject project, [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService, IProjectAsyncLoadDashboard loadDashboard)
        {
            _tasksService = tasksService;
            _loadDashboard = loadDashboard;

            ProjectGuidServices = new OrderPrecedenceImportCollection<IProjectGuidService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectGuidService> ProjectGuidServices
        {
            get;
        }

        public async Task<Guid> GetProjectGuidAsync()
        {
            await _loadDashboard.ProjectLoadedInHostWithCancellation(_tasksService)
                                .ConfigureAwait(false);

            IProjectGuidService projectGuidService = ProjectGuidServices.FirstOrDefault()?.Value;
            if (projectGuidService == null)
                return Guid.Empty;

            if (projectGuidService is IProjectGuidService2 projectGuidService2)
            {
                return await projectGuidService2.GetProjectGuidAsync()
                                                .ConfigureAwait(false);
            }

            return projectGuidService.ProjectGuid;
        }
    }
}
