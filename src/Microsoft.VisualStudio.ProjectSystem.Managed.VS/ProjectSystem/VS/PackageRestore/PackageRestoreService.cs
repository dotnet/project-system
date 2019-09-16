// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Responsible for pushing ("nominating") project data such as referenced packages and 
    ///     target frameworks to NuGet so that it can perform a package restore.
    /// </summary>
    [Export(typeof(IPackageRestoreService))]
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class PackageRestoreService : AbstractMultiLifetimeComponent<PackageRestoreService.PackageRestoreServiceInstance>, IProjectDynamicLoadComponent, IPackageRestoreService
    {
        private readonly UnconfiguredProject _project;
        private readonly IPackageRestoreUnconfiguredInputDataSource _dataSource;
        private readonly IProjectThreadingService _threadingService;
        private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;
        private readonly IVsSolutionRestoreService3 _solutionRestoreService;
        private readonly IFileSystem _fileSystem;
        private readonly IProjectLogger _logger;

        private IReceivableSourceBlock<IProjectVersionedValue<RestoreData>>? _publicBlock;
        private IBroadcastBlock<IProjectVersionedValue<RestoreData>>? _broadcastBlock;

        [ImportingConstructor]
        public PackageRestoreService(
            UnconfiguredProject project,
            IPackageRestoreUnconfiguredInputDataSource dataSource,
            IProjectThreadingService threadingService,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService projectAsynchronousTasksService,
            IVsSolutionRestoreService3 solutionRestoreService,
            IFileSystem fileSystem,
            IProjectLogger logger)
            : base(threadingService.JoinableTaskContext)
        {
            _project = project;
            _dataSource = dataSource;
            _threadingService = threadingService;
            _projectAsynchronousTasksService = projectAsynchronousTasksService;
            _solutionRestoreService = solutionRestoreService;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public IReceivableSourceBlock<IProjectVersionedValue<RestoreData>> RestoreData
        {
            get
            {
                EnsureInitialized();

                return _publicBlock!;
            }
        }

        protected override PackageRestoreServiceInstance CreateInstance()
        {
            Assumes.NotNull(_broadcastBlock);

            return new PackageRestoreServiceInstance(
                _project,
                _dataSource,
                _threadingService,
                _projectAsynchronousTasksService,
                _solutionRestoreService,
                _fileSystem,
                _logger,
                _broadcastBlock);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await base.InitializeCoreAsync(cancellationToken);

            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<RestoreData>>();
            _publicBlock = _broadcastBlock.SafePublicize();
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            await base.DisposeCoreAsync(initialized);

            _broadcastBlock?.Complete();
        }

        private void EnsureInitialized()
        {
            _threadingService.ExecuteSynchronously(() => InitializeAsync(CancellationToken.None));
        }
    }
}
