// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Responsible for pushing ("nominating") project data such as referenced packages and 
    ///     target frameworks to NuGet so that it can perform a package restore.
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class PackageRestoreInitiator : AbstractProjectDynamicLoadComponent
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IVsSolutionRestoreService _solutionRestoreService;
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IProjectLogger _logger;
        
        [ImportingConstructor]
        public PackageRestoreInitiator(
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
            return new PackageRestoreInitiatorInstance(_projectVsServices, _solutionRestoreService, _activeConfiguredProjectSubscriptionService, _activeConfigurationGroupService, _logger);
        }
    }
}
