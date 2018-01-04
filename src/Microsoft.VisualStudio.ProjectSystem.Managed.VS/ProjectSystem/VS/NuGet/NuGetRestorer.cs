// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class NuGetRestorer : AbstractProjectDynamicLoadComponent
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IVsSolutionRestoreService _solutionRestoreService;
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IProjectLogger _logger;
        
        [ImportingConstructor]
        public NuGetRestorer(
            IUnconfiguredProjectVsServices projectVsServices,
            IVsSolutionRestoreService solutionRestoreService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService,
            IActiveConfigurationGroupService activeConfigurationGroupService,
            IProjectLogger logger) 
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            _solutionRestoreService = solutionRestoreService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
            _activeConfigurationGroupService = activeConfigurationGroupService;
            _logger = logger;
        }

        protected override AbstractProjectDynamicLoadInstance CreateInstance()
        {
            return new NuGetRestorerInstance(_projectVsServices, _solutionRestoreService, _activeConfiguredProjectSubscriptionService, _activeConfigurationGroupService, _logger);
        }
    }
}
