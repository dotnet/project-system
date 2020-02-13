// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Responsible for reporting package restore progress to operation progress.
    /// </summary>
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class PackageRestoreProgressTracker : AbstractMultiLifetimeComponent<PackageRestoreProgressTracker.PackageRestoreProgressTrackerInstance>, IProjectDynamicLoadComponent
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectThreadingService _threadingService;
        private readonly IProjectFaultHandlerService _projectFaultHandlerService;
        private readonly IDataProgressTrackerService _dataProgressTrackerService;
        private readonly IPackageRestoreDataSource _dataSource;
        private readonly IProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        public PackageRestoreProgressTracker(
            ConfiguredProject project,
            IProjectThreadingService threadingService,
            IProjectFaultHandlerService projectFaultHandlerService,
            IDataProgressTrackerService dataProgressTrackerService,
            IPackageRestoreDataSource dataSource,
            IProjectSubscriptionService projectSubscriptionService)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _threadingService = threadingService;
            _projectFaultHandlerService = projectFaultHandlerService;
            _dataProgressTrackerService = dataProgressTrackerService;
            _dataSource = dataSource;
            _projectSubscriptionService = projectSubscriptionService;
        }

        protected override PackageRestoreProgressTrackerInstance CreateInstance()
        {
            return new PackageRestoreProgressTrackerInstance(
                _project,
                _threadingService,
                _projectFaultHandlerService,
                _dataProgressTrackerService,
                _dataSource,
                _projectSubscriptionService);
        }
    }
}
