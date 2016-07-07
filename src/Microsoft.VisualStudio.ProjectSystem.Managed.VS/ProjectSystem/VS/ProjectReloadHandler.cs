// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    // <summary>
    // ProjectReloadHandler
    //
    // Autoloaded component which monitors the file system for changes to the project file and updates the project with the changes
    // without actually doing a project reload. It uses the VS file change service to monitor the project file and also listens to 
    // solution events so that it can detect when a project file rename occurs and it can start watching a new file.
    // </summary>
    [Export(typeof(ProjectReloadHandler))]
    [AppliesTo("HandlesOwnReload")]
    internal class ProjectReloadHandler : OnceInitializedOnceDisposedAsync, IVsFileChangeEvents, IVsSolutionEvents, IVsSolutionEvents4
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IServiceProvider _serviceProvider;

        private uint  _projectFileFCNCookie = VSConstants.VSCOOKIE_NIL;
        private uint  _solutionEventsCookie = VSConstants.VSCOOKIE_NIL;
        private string _projectFileBeingMonitored;
        private ITaskDelayScheduler _reloadDelayScheduler;

        private const int ReloadDelay = 1000;   // delay 1s before applying updated project contents.

        [ImportingConstructor]
        public ProjectReloadHandler(IUnconfiguredProjectVsServices projectVsServices, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Autoload entry point. Once the project factory has returned to VS, the component will be loaded by CPS. There is no
        /// expectation that this component will be imported by 
        /// </summary>
        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo("HandlesOwnReload")]
        private Task Initialize()
        {
            return InitializeCoreAsync(CancellationToken.None);
        }

        /// <summary>
        /// Adds a file change watcher on the project file.
        /// </summary>
        private async Task MonitorProjectFileAsync(string projectFile)
        {
            await _projectVsServices.ThreadingService.SwitchToUIThread();

            if (_projectFileFCNCookie == VSConstants.VSCOOKIE_NIL)
            {
                IVsFileChangeEx fileChangeService = _serviceProvider.GetService<IVsFileChangeEx, SVsFileChangeEx>();
                if (fileChangeService != null)
                {
                    int hr = fileChangeService.AdviseFileChange(projectFile, (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size), this, out _projectFileFCNCookie);
                    System.Diagnostics.Debug.Assert(ErrorHandler.Succeeded(hr) && _projectFileFCNCookie != VSConstants.VSCOOKIE_NIL);
                    _projectFileBeingMonitored = projectFile;
                }
            }
        }

        /// <summary>
        /// Removes the file change watch on the project file. 
        /// </summary>
        private async Task StopMonitoringProjectfileAsync()
        {
            if(_projectFileFCNCookie != VSConstants.VSCOOKIE_NIL)
            {
                await _projectVsServices.ThreadingService.SwitchToUIThread();

                // Remove watch
                IVsFileChangeEx fileChangeService = _serviceProvider.GetService<IVsFileChangeEx, SVsFileChangeEx>();
                if (fileChangeService != null)
                {
                    int hr = fileChangeService.UnadviseFileChange(_projectFileFCNCookie);
                    System.Diagnostics.Debug.Assert(ErrorHandler.Succeeded(hr));
                    _projectFileFCNCookie = VSConstants.VSCOOKIE_NIL;
                }
            }
        }

        /// <summary>
        /// Adds a file change watcher on the project file.
        /// </summary>
        private async Task ConnectToSolutionEvents()
        {
            await _projectVsServices.ThreadingService.SwitchToUIThread();

            if (_solutionEventsCookie == VSConstants.VSCOOKIE_NIL)
            {
                IVsSolution solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
                if (solution != null)
                {
                    int hr = solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
                    System.Diagnostics.Debug.Assert(ErrorHandler.Succeeded(hr) && _solutionEventsCookie != VSConstants.VSCOOKIE_NIL);
                }
            }
        }

        /// <summary>
        /// Removes the file change watch on the project file. 
        /// </summary>
        private async Task DisconnectFromSolutionEvents()
        {
            if(_solutionEventsCookie != VSConstants.VSCOOKIE_NIL)
            {
                await _projectVsServices.ThreadingService.SwitchToUIThread();

                IVsSolution solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
                if (solution != null)
                {
                    int hr = solution.UnadviseSolutionEvents(_solutionEventsCookie);
                    System.Diagnostics.Debug.Assert(ErrorHandler.Succeeded(hr));
                    _solutionEventsCookie = VSConstants.VSCOOKIE_NIL;
                }
            }
        }

        /// <summary>
        /// Handles one time initialization
        /// </summary>
        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _reloadDelayScheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(ReloadDelay), _projectVsServices.ThreadingService, CancellationToken.None);
            await ConnectToSolutionEvents().ConfigureAwait(false);

            await MonitorProjectFileAsync(_projectVsServices.Project.FullPath).ConfigureAwait(false);
        }

        /// <summary>
        /// IDispoable handler. Should only be called once
        /// </summary>
        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if(_reloadDelayScheduler != null)
            {
                _reloadDelayScheduler.Dispose();
                _reloadDelayScheduler = null;
            }
            await DisconnectFromSolutionEvents().ConfigureAwait(false);
            await StopMonitoringProjectfileAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Called when the project file changes. In our case since we only watch one file, the list of files
        /// should be one.
        /// </summary>
        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] grfChange)
        {
            if(cChanges == 1 && (grfChange[0] & (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Time)) != 0 && 
              rgpszFile[0].Equals(_projectVsServices.Project.FullPath))
            {
                _reloadDelayScheduler.ScheduleAsyncTask(async (ct) => 
                {
                    // Grab the UI thread so that we block until the reload completes
                    await _projectVsServices.ThreadingService.SwitchToUIThread();

                    ReloadProjectResult result = _projectVsServices.ThreadingService.ExecuteSynchronously(async () =>
                    {
                        return await  TryToAutoReloadProject(ct).ConfigureAwait(true);
                    });


                    // If a reload was not completed do a normal solution level reload.
                    if(result == ReloadProjectResult.ReloadRequired || result == ReloadProjectResult.ReloadRequiredProjectDirty)
                    {
                        ReloadProjectInSolution(result == ReloadProjectResult.ReloadRequiredProjectDirty);
                    }
                });
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Function called after a project file change has been detected which pushes the changes to CPS. The return value indicates the status of the 
        /// reload. ifrefresh of the msbuild contens was s returns false, it means the reload was not
        /// done or not necssary and a solution level reload is necessary.
        /// </summary>
        private async Task<ReloadProjectResult> TryToAutoReloadProject(CancellationToken cancelToken)
        {
            // The file better not have changed at this point
            if(cancelToken.IsCancellationRequested || !string.Equals(_projectFileBeingMonitored, _projectVsServices.Project.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                // Nothing to do
                return ReloadProjectResult.NoAction;
            }

            // We need a write lock to modify the project file contents.
            using (var writeAccess = await _projectVsServices.ProjectLockService.WriteLockAsync())
            {
                await writeAccess.CheckoutAsync(_projectVsServices.Project.FullPath).ConfigureAwait(true);
                var msbuildProject = await writeAccess.GetProjectXmlAsync(_projectVsServices.Project.FullPath, cancelToken).ConfigureAwait(true);
                if(msbuildProject.HasUnsavedChanges)
                {
                    // For now force a solution reload.
                    return ReloadProjectResult.ReloadRequiredProjectDirty;
                }
                else
                {
                    // What we need to do is load the one on disk. This accomplishes two things: 1) verifies that at least the msbuild is OK. If it isn't
                    // we want to let the normal solution reload occur so that the user sees the failure, and 2) it allows us to clear the current project
                    // and do a deepcopy from the newly loaded one to the original - replacing its contents. Note that the Open call is cached so that if
                    // the project is already in a collection, it will just return the cached one. For this reason, and the fact we don't want the project to
                    // appear in the global project collection, the file is opened in a new collection which we will discard wnen done.
                    try
                    {
                        ProjectCollection thisCollection = new ProjectCollection();
                        var newProject = Microsoft.Build.Construction.ProjectRootElement.Open(_projectVsServices.Project.FullPath, thisCollection);

                        msbuildProject.RemoveAllChildren();
                        msbuildProject.DeepCopyFrom(newProject);

                        // There isn't a way to clear the dirty flag on the project xml, so to work around that the project is saved
                        // to a StringWriter. 
                        var tw = new StringWriter();
                        msbuildProject.Save(tw);

                        thisCollection.UnloadAllProjects();
                    }
                    catch (Microsoft.Build.Exceptions.InvalidProjectFileException)
                    {
                        // Indicate we weren't able to complete the action. We want to do a normal reload 
                        return ReloadProjectResult.ReloadRequired;
                    }
                    catch (Exception ex)
                    {
                        // Any other exception likely mean the msbuildProject is not in a good state. Example, DeepCopyFrom failed after 
                        // RemoveAll children. The only safe thing to do at this point is to reload the project in the solution
                        System.Diagnostics.Debug.Assert(false, "Replace xml failed with: " + ex.Message);
                        return ReloadProjectResult.ReloadRequired;
                    }
                }
            }

            // It is important to wait for the new tree to be published to ensure all updates have occurred before we
            // release our hold on the UI thread. This prevents unnecessary race conditions and prevent the user
            // from interacting with the project until the evaluation has completed.
            await _projectVsServices.ProjectTree.TreeService.PublishLatestTreeAsync(blockDuringLoadingTree: true).ConfigureAwait(false);
            
            return ReloadProjectResult.ReloadCompleted;
        }

        /// <summary>
        /// Helper to use the solution to reload the project.
        /// Reloading is managed via the ReloadItem() method of our parent hierarhcy(solution 
        /// or solution folder).  So first we get our parent hierarchy and our itemid in the parent
        /// hierarchy. 
        /// </summary>
        void ReloadProjectInSolution(bool projectDirty)
        {
            // Get our parent hierarchy and our itemid in the parent hierarchy.
            IVsHierarchy parentHier = _projectVsServices.VsHierarchy.GetProperty<IVsHierarchy>(VsHierarchyPropID.ParentHierarchy, null);
            if(parentHier == null)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_UNEXPECTED);
            }
            uint parentItemid  = (uint)_projectVsServices.VsHierarchy.GetProperty<int>(VsHierarchyPropID.ParentHierarchyItemid, unchecked((int)VSConstants.VSITEMID_NIL));
            if(parentItemid == VSConstants.VSITEMID_NIL)
            {
                ErrorHandler.ThrowOnFailure(VSConstants.E_UNEXPECTED);
            }
            
            // Now using IVsPersistHierarchyItem2 we reload the project.
            int hr = ((IVsPersistHierarchyItem2)parentHier).ReloadItem((uint)parentItemid, dwReserved: 0);
            ErrorHandler.ThrowOnFailure(hr);
        }

        /// <summary>
        /// Callback for directory changes. Since we don't watch the folder there is nothing to do here
        /// </summary>
        public int DirectoryChanged(string pszDirectory)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// IVsSolutionEvents4. We only care about OnAfterRenameProject. If our project file is renamed we need to
        /// stop watching the old file and start watching the new file
        /// </summary>
        public int OnAfterRenameProject(IVsHierarchy pHierarchy)
        {
            if(_projectFileBeingMonitored != null && pHierarchy == _projectVsServices.VsHierarchy)
            {
                // We don't care about case changes 
                if(_projectVsServices.Project.FullPath.Equals(_projectFileBeingMonitored, StringComparison.OrdinalIgnoreCase))
                {
                    _projectVsServices.ThreadingService.ExecuteSynchronously(async () =>
                    {
                        await StopMonitoringProjectfileAsync().ConfigureAwait(false);
                        await MonitorProjectFileAsync(_projectVsServices.Project.FullPath).ConfigureAwait(false);
                    });
                }
            }
            return VSConstants.S_OK;
        }

        public int OnQueryChangeProjectParent(IVsHierarchy pHierarchy, IVsHierarchy pNewParentHier, ref int pfCancel)
        {
            pfCancel = 0;
            return VSConstants.S_OK;
        }

        public int OnAfterChangeProjectParent(IVsHierarchy pHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAsynchOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// IVsSolutionEvents memebers. Needed to implement this 
        /// </summary>
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            pfCancel = 0;
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            pfCancel = 0;
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            pfCancel = 0;
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }
    }
}
