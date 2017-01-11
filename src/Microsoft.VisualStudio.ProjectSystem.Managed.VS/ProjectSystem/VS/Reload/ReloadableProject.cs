// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    // <summary>
    // ProjectReloadHandler
    //
    // Auto-loaded component which represents a project which can auto-reload without going through the normal solution level reload. Upon load it 
    // registers itself with the IProjectReloadManager which is the component which monitors for file changes and calls back on the this object to perform
    // the actual reload operation. 
    [Export(typeof(ReloadableProject))]
    [AppliesTo("HandlesOwnReload")]
    internal class ReloadableProject : OnceInitializedOnceDisposedAsync, IReloadableProject
    {

        [ImportingConstructor]
        public ReloadableProject(IUnconfiguredProjectVsServices projectVsServices, IProjectReloadManager reloadManager)
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            _reloadManager = reloadManager;
        }

        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IProjectReloadManager _reloadManager;

        public string ProjectFile
        {
            get
            {
                return _projectVsServices.Project.FullPath;
            }
        }

        public IVsHierarchy VsHierarchy
        {
            get
            {
                return _projectVsServices.VsHierarchy;
            }
        }

        /// <summary>
        /// Auto-load entry point. Once the project factory has returned to VS, the component will be loaded by CPS. There is no
        /// expectation that this component will be imported by any other component
        /// </summary>
        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo("HandlesOwnReload")]
        public Task Initialize()
        {
            return InitializeCoreAsync(CancellationToken.None);
        }

        /// <summary>
        /// Handles one time initialization by registering with the reload manager
        /// </summary>
        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _reloadManager.RegisterProjectAsync(this).ConfigureAwait(false);
        }

        /// <summary>
        /// IDispoable handler. Should only be called once
        /// </summary>
        protected override async Task DisposeCoreAsync(bool initialized)
        {
            await _reloadManager.UnregisterProjectAsync(this).ConfigureAwait(false);
        }

        /// <summary>
        /// Function called after a project file change has been detected which pushes the changes to CPS. The return value indicates the status of the 
        /// reload. 
        /// </summary>
        public async Task<ProjectReloadResult> ReloadProjectAsync()
        {
            // We need a write lock to modify the project file contents. Note that all awaits while holding the lock need
            // to capture the context as the project lock service has a special execution context which ensures only a single
            // thread has access.
            using (var writeAccess = await _projectVsServices.ProjectLockService.WriteLockAsync())
            {
                await writeAccess.CheckoutAsync(_projectVsServices.Project.FullPath).ConfigureAwait(true);
                var msbuildProject = await writeAccess.GetProjectXmlAsync(_projectVsServices.Project.FullPath, CancellationToken.None).ConfigureAwait(true);
                if (msbuildProject.HasUnsavedChanges)
                {
                    // For now force a solution reload.
                    return ProjectReloadResult.ReloadFailedProjectDirty;
                }
                else
                {
                    try
                    {
                        // This reloads the project off disk and handles if the new XML is invalid
                        msbuildProject.Reload();
                    }
                    catch (Microsoft.Build.Exceptions.InvalidProjectFileException)
                    {
                        // Indicate we weren't able to complete the action. We want to do a normal reload
                        return ProjectReloadResult.ReloadFailed;
                    }
                    catch (Exception ex)
                    {
                        // Any other exception likely mean the msbuildProject is not in a good state. Example, DeepCopyFrom failed.
                        // The only safe thing to do at this point is to reload the project in the solution
                        // TODO: should we have an additional return value here to indicate that the existing project could be in a bad
                        // state and the reload needs to happen without the user being able to block it?
                        System.Diagnostics.Debug.Assert(false, "Replace xml failed with: " + ex.Message);
                        return ProjectReloadResult.ReloadFailed;
                    }
                }
            }

            // It is important to wait for the new tree to be published to ensure all updates have occurred before the reload manager
            // releases its hold on the UI thread. This prevents unnecessary race conditions and prevent the user
            // from interacting with the project until the evaluation has completed.
            await _projectVsServices.ProjectTree.TreeService.PublishLatestTreeAsync(blockDuringLoadingTree: true).ConfigureAwait(false);
            
            return ProjectReloadResult.ReloadCompleted;
        }
    }
}
