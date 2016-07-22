// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
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
    [Export(typeof(ReloadableProject))]
    [AppliesTo("HandlesOwnReload")]
    internal class ReloadableProject : OnceInitializedOnceDisposedAsync, IReloadableProject
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IServiceProvider _serviceProvider;
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

        [ImportingConstructor]
        public ReloadableProject(IUnconfiguredProjectVsServices projectVsServices, IProjectReloadManager reloadManager, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(projectVsServices.ThreadingService.JoinableTaskContext)
        {
            _projectVsServices = projectVsServices;
            _serviceProvider = serviceProvider;
            _reloadManager = reloadManager;
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
        /// reload. ifrefresh of the msbuild contens was s returns false, it means the reload was not
        /// done or not necssary and a solution level reload is necessary.
        /// </summary>
        public async Task<ProjectReloadResult> ReloadProjectAsync()
        {
            // We need a write lock to modify the project file contents. Note that all awaits while holdimg the lock need
            // to capture the context as the project lock service has a special execution context which ensures only a single
            // thread has access.
            using (var writeAccess = await _projectVsServices.ProjectLockService.WriteLockAsync())
            {
                await writeAccess.CheckoutAsync(_projectVsServices.Project.FullPath).ConfigureAwait(true);
                var msbuildProject = await writeAccess.GetProjectXmlAsync(_projectVsServices.Project.FullPath, CancellationToken.None).ConfigureAwait(true);
                if(msbuildProject.HasUnsavedChanges)
                {
                    // For now force a solution reload.
                    return ProjectReloadResult.ReloadFailedProjectDirty;
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
                        var newProject = ProjectRootElement.Open(_projectVsServices.Project.FullPath, thisCollection);

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
                        return ProjectReloadResult.ReloadFailed;
                    }
                    catch (Exception ex)
                    {
                        // Any other exception likely mean the msbuildProject is not in a good state. Example, DeepCopyFrom failed after 
                        // RemoveAll children. The only safe thing to do at this point is to reload the project in the solution
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
