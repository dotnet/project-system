// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.Logging;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Responsible for pushing ("nominating") project data such as referenced packages and 
    ///     target frameworks to NuGet so that it can perform a package restore.
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class PackageRestoreInitiator : AbstractMultiLifetimeComponent<PackageRestoreInitiator.PackageRestoreInitiatorInstance>, IProjectDynamicLoadComponent
    {
        private readonly UnconfiguredProject _project;
        private readonly IPackageRestoreUnconfiguredDataSource _dataSource;
        private readonly IProjectThreadingService _threadingService;
        private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;
        private readonly IVsSolutionRestoreService3 _solutionRestoreService;
        private readonly IProjectLogger _logger;

        [ImportingConstructor]
        public PackageRestoreInitiator(
            UnconfiguredProject project,
            IPackageRestoreUnconfiguredDataSource dataSource,
            IProjectThreadingService threadingService,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService projectAsynchronousTasksService,
            IVsSolutionRestoreService3 solutionRestoreService,
            IProjectLogger logger)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _dataSource = dataSource;
            _threadingService = threadingService;
            _projectAsynchronousTasksService = projectAsynchronousTasksService;
            _solutionRestoreService = solutionRestoreService;
            _logger = logger;
        }

        protected override PackageRestoreInitiatorInstance CreateInstance()
        {
            return new PackageRestoreInitiatorInstance(
                _project,
                _dataSource,
                _threadingService,
                _projectAsynchronousTasksService,
                _solutionRestoreService,
                _logger);
        }
    }
}
