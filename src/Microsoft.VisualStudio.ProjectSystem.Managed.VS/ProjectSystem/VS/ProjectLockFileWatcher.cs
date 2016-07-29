// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class ProjectLockFileWatcher : OnceInitializedOnceDisposed, IVsFileChangeEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnconfiguredProjectCommonServices _projectServices;
        private uint _filechangeCookie;
        private readonly IProjectLockService _projectLockService;

        [ImportingConstructor]
        public ProjectLockFileWatcher([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, IUnconfiguredProjectCommonServices projectServices, IProjectLockService projectLockService)
        {
            _serviceProvider = serviceProvider;
            _projectServices = projectServices;
            _projectLockService = projectLockService;
        }

        [ConfiguredProjectAutoLoad]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        private void Load()
        {
            this.EnsureInitialized();
        }
        
        protected override void Initialize()
        {
            // Look for a project.lock.json file. If there isn't one in the project, there's nothing to do.
            var projectDirectory = Path.GetDirectoryName(_projectServices.Project.FullPath);
            var projectLockJsonFilePath = PathHelper.Combine(projectDirectory, "project.lock.json");

            IVsFileChangeEx fileChangeService = _serviceProvider.GetService<IVsFileChangeEx, SVsFileChangeEx>();
            if (fileChangeService != null)
            {
                int hr = fileChangeService.AdviseFileChange(projectLockJsonFilePath, (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size), this, out _filechangeCookie);
            }
        }
        
        protected override void Dispose(bool disposing)
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

        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            _projectServices.ThreadingService.ExecuteSynchronously(async () => { 
                using (var access = await _projectLockService.UpgradeableReadLockAsync())
                {
                    var project = await access.GetProjectAsync(_projectServices.ActiveConfiguredProject);

                    using (await _projectLockService.WriteLockAsync())
                    {
                        project.MarkDirty();
                        _projectServices.ActiveConfiguredProject.NotifyProjectChange();
                    }
                }
            });

            return VSConstants.S_OK;
        }

        public int DirectoryChanged(string pszDirectory)
        {
            return VSConstants.S_OK;
        }
    }
}
