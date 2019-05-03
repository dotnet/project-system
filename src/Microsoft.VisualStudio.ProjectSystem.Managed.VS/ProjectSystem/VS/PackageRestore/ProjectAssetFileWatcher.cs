// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Watches for writes to the project.assets.json, triggering a evaluation if it changes.
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class ProjectAssetFileWatcher : AbstractMultiLifetimeComponent<ProjectAssetFileWatcher.ProjectAssetFileWatcherInstance>, IProjectDynamicLoadComponent
    {
        private readonly IVsService<IVsAsyncFileChangeEx> _fileChangeService;
        private readonly IUnconfiguredProjectCommonServices _projectServices;
        private readonly IUnconfiguredProjectTasksService _projectTasksService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IProjectTreeProvider _fileSystemTreeProvider;

        [ImportingConstructor]
        public ProjectAssetFileWatcher(
          IVsService<SVsFileChangeEx, IVsAsyncFileChangeEx> fileChangeService,
          [Import(ContractNames.ProjectTreeProviders.FileSystemDirectoryTree)]IProjectTreeProvider fileSystemTreeProvider,
          IUnconfiguredProjectCommonServices projectServices,
          IUnconfiguredProjectTasksService projectTasksService,
          IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)
          : base(projectServices.ThreadingService.JoinableTaskContext)
        {
            _fileChangeService = fileChangeService;
            _fileSystemTreeProvider = fileSystemTreeProvider;
            _projectServices = projectServices;
            _projectTasksService = projectTasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
        }

        protected override ProjectAssetFileWatcherInstance CreateInstance()
        {
            return new ProjectAssetFileWatcherInstance(
                _fileChangeService,
                _fileSystemTreeProvider,
                _projectServices,
                _projectTasksService,
                _activeConfiguredProjectSubscriptionService);
        }
    }
}
