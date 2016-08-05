// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class ProjectLockFileWatcher : OnceInitializedOnceDisposed, IVsFileChangeEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnconfiguredProjectCommonServices _projectServices;
        private readonly IProjectLockService _projectLockService;
        private readonly IProjectTreeProvider _fileSystemTreeProvider;
        private IDisposable _treeWatcher;
        private uint _filechangeCookie;
        private string _fileBeingWatched;

        [ImportingConstructor]
        public ProjectLockFileWatcher([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
                                      [Import("Microsoft.VisualStudio.ProjectSystem.FileSystemDirectory")] IProjectTreeProvider fileSystemTreeProvider,
                                      IUnconfiguredProjectCommonServices projectServices,
                                      IProjectLockService projectLockService)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(fileSystemTreeProvider, nameof(fileSystemTreeProvider));
            Requires.NotNull(projectServices, nameof(projectServices));
            Requires.NotNull(projectLockService, nameof(projectLockService));

            _serviceProvider = serviceProvider;
            _fileSystemTreeProvider = fileSystemTreeProvider;
            _projectServices = projectServices;
            _projectLockService = projectLockService;
        }

        [ConfiguredProjectAutoLoad]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        internal void Load()
        {
            this.EnsureInitialized();
        }
        
        protected override void Initialize()
        {
            _treeWatcher = _fileSystemTreeProvider.Tree.LinkTo(new ActionBlock<IProjectVersionedValue<IProjectTreeSnapshot>>(new Action<IProjectVersionedValue<IProjectTreeSnapshot>>(this.ProjectTree_ChangedAsync)));
        }

        internal void ProjectTree_ChangedAsync(IProjectVersionedValue<IProjectTreeSnapshot> treeSnapshot)
        {
            var newTree = treeSnapshot.Value.Tree;
            if (newTree == null)
            {
                return;
            }

            // If there' no project.json in the project, there's nothing to watch.
            IProjectTree projectJsonNode = FindProjectJsonNode(newTree);
            if (projectJsonNode == null)
            {
                // project.json may have been deleted.
                UnregisterFileWatcherIfAny();
                return;
            }

            var projectDirectory = Path.GetDirectoryName(_projectServices.Project.FullPath);
            var projectLockJsonFilePath = Path.ChangeExtension(PathHelper.Combine(projectDirectory, projectJsonNode.Caption), ".lock.json");

            // project.json may have been renamed to {projectName}.project.json. In that case change the file watcher.
            if (!PathHelper.IsSamePath(projectLockJsonFilePath, _fileBeingWatched))
            {
                UnregisterFileWatcherIfAny();
                RegisterFileWatcher(projectLockJsonFilePath);
                _fileBeingWatched = projectLockJsonFilePath;
            }
        }

        private IProjectTree FindProjectJsonNode(IProjectTree newTree)
        {
            IProjectTree projectJsonNode;
            if (newTree.TryFindImmediateChild("project.json", out projectJsonNode))
            {
                return projectJsonNode;
            }

            var projectName = Path.GetFileNameWithoutExtension(_projectServices.Project.FullPath);
            if (newTree.TryFindImmediateChild($"{projectName}.project.json", out projectJsonNode))
            {
                return projectJsonNode;
            }

            return null;
        }

        private void RegisterFileWatcher(string projectLockJsonFilePath)
        {
            IVsFileChangeEx fileChangeService = _serviceProvider.GetService<IVsFileChangeEx, SVsFileChangeEx>();
            if (fileChangeService != null)
            {
                int hr = fileChangeService.AdviseFileChange(projectLockJsonFilePath, (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Add | _VSFILECHANGEFLAGS.VSFILECHG_Del), this, out _filechangeCookie);
            }
        }

        private void UnregisterFileWatcherIfAny()
        {
            if (_filechangeCookie != VSConstants.VSCOOKIE_NIL)
            {
                IVsFileChangeEx fileChangeService = _serviceProvider.GetService<IVsFileChangeEx, SVsFileChangeEx>();
                if (fileChangeService != null)
                {
                    int hr = fileChangeService.UnadviseFileChange(_filechangeCookie);
                    System.Diagnostics.Debug.Assert(ErrorHandler.Succeeded(hr));
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _treeWatcher.Dispose();
                UnregisterFileWatcherIfAny();
            }
        }

        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            // Kick off the operation to notify the project change in a different thread.
            _projectServices.ThreadingService.Fork(async () => { 
                using (var access = await _projectLockService.WriteLockAsync())
                {
#pragma warning disable CA2007 // Inside a write lock, we should get back to the same thread.
                    var project = await access.GetProjectAsync(_projectServices.ActiveConfiguredProject);
#pragma warning restore CA2007 // Do not directly await a Task
                    project.MarkDirty();
                    _projectServices.ActiveConfiguredProject.NotifyProjectChange();
                }
            }, configuredProject: _projectServices.ActiveConfiguredProject);

            return VSConstants.S_OK;
        }

        public int DirectoryChanged(string pszDirectory)
        {
            return VSConstants.S_OK;
        }
    }
}
