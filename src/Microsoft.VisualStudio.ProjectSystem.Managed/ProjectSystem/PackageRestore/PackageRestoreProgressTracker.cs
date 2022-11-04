// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Responsible for reporting package restore progress to operation progress.
    /// </summary>
    [Export(typeof(IImplicitlyActiveConfigurationComponent))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class PackageRestoreProgressTracker : AbstractMultiLifetimeComponent<PackageRestoreProgressTracker.PackageRestoreProgressTrackerInstance>, IImplicitlyActiveConfigurationComponent
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

        public Task ActivateAsync()
        {
            return LoadAsync();
        }

        public Task DeactivateAsync()
        {
            return UnloadAsync();
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
