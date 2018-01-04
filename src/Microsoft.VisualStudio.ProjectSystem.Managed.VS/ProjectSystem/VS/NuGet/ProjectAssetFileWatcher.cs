// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Watches for writes to the project.assets.json, triggering a evaluation if it changes.
    /// </summary>
    internal class ProjectAssetFileWatcher : OnceInitializedOnceDisposedAsync, IVsFileChangeEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnconfiguredProjectCommonServices _projectServices;
        private readonly IProjectLockService _projectLockService;
        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IProjectTreeProvider _fileSystemTreeProvider;
        private IVsFileChangeEx _fileChangeService;
        private IDisposable _treeWatcher;
        private uint _filechangeCookie;
        private string _fileBeingWatched;

        [ImportingConstructor]
        public ProjectAssetFileWatcher([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
                                      [Import(ContractNames.ProjectTreeProviders.FileSystemDirectoryTree)] IProjectTreeProvider fileSystemTreeProvider,
                                      IUnconfiguredProjectCommonServices projectServices,
                                      IProjectLockService projectLockService,
                                      IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)
            : base(projectServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(fileSystemTreeProvider, nameof(fileSystemTreeProvider));
            Requires.NotNull(projectServices, nameof(projectServices));
            Requires.NotNull(projectLockService, nameof(projectLockService));
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));

            _serviceProvider = serviceProvider;
            _fileSystemTreeProvider = fileSystemTreeProvider;
            _projectServices = projectServices;
            _projectLockService = projectLockService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
        }

        /// <summary>
        /// Called on project load.
        /// </summary>
        [ConfiguredProjectAutoLoad(RequiresUIThread = false)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
        internal void Load()
        {
            InitializeAsync();
        }

        /// <summary>
        /// Initialize the watcher.
        /// </summary>
        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _fileChangeService = (IVsFileChangeEx)(await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsFileChangeEx)).ConfigureAwait(false));

            // The tree source to get changes to the tree so that we can identify when the assets file changes.
            var treeSource = _fileSystemTreeProvider.Tree.SyncLinkOptions();

            // The property source used to get the value of the $ProjectAssetsFile property so that we can identify the location of the assets file.
            var sourceLinkOptions = new StandardRuleDataflowLinkOptions
            {
                RuleNames = Empty.OrdinalIgnoreCaseStringSet.Add(ConfigurationGeneral.SchemaName),
                PropagateCompletion = true
            };
            var propertySource = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(sourceLinkOptions);
            var target = new ActionBlock<IProjectVersionedValue<Tuple<IProjectTreeSnapshot, IProjectSubscriptionUpdate>>>(DataFlow_Changed);

            // Join the two sources so that we get synchronized versions of the data.
            _treeWatcher = ProjectDataSources.SyncLinkTo(treeSource, propertySource, target);
        }

        /// <summary>
        /// Called on changes to the project tree.
        /// </summary>
        internal async Task DataFlow_Changed(IProjectVersionedValue<Tuple<IProjectTreeSnapshot, IProjectSubscriptionUpdate>> dataFlowUpdate)
        {
            await InitializeAsync().ConfigureAwait(false);

            var treeSnapshot = dataFlowUpdate.Value.Item1;
            var newTree = treeSnapshot.Tree;
            if (newTree == null)
            {
                return;
            }

            // If tree changed when we are disposing then ignore the change.
            if (IsDisposing)
            {
                return;
            }

            // NOTE: Project lock file path may be null
            var projectUpdate = dataFlowUpdate.Value.Item2;
            var projectLockFilePath = GetProjectAssetsFilePath(newTree, projectUpdate);

            // project.json may have been renamed to {projectName}.project.json or in the case of the project.assets.json,
            // the immediate path could have changed. In either case, change the file watcher.
            if (!PathHelper.IsSamePath(projectLockFilePath, _fileBeingWatched))
            {
                UnregisterFileWatcherIfAny();
                RegisterFileWatcherAsync(projectLockFilePath);
            }
        }

        private string GetProjectAssetsFilePath(IProjectTree newTree, IProjectSubscriptionUpdate projectUpdate)
        {
            var projectFilePath = projectUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, null);

            // First check to see if the project has a project.json.
            IProjectTree projectJsonNode = FindProjectJsonNode(newTree, projectFilePath);
            if (projectJsonNode != null)
            {
                var projectDirectory = Path.GetDirectoryName(projectFilePath);
                var projectLockJsonFilePath = Path.ChangeExtension(PathHelper.Combine(projectDirectory, projectJsonNode.Caption), ".lock.json");
                return projectLockJsonFilePath;
            }

            // If there is no project.json then get the patch to obj\project.assets.json file which is generated for projects
            // with <PackageReference> items.
            var objDirectory = projectUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.BaseIntermediateOutputPathProperty, null);

            if (string.IsNullOrEmpty(objDirectory))
            {   // Don't have an intermdiate directory set, probably missing SDK attribute or Microsoft.Common.props

                return null;
            }

            objDirectory = PathHelper.MakeRooted(projectFilePath, objDirectory);
            var projectAssetsFilePath = PathHelper.Combine(objDirectory, "project.assets.json");
            return projectAssetsFilePath;
        }

        private IProjectTree FindProjectJsonNode(IProjectTree newTree, string projectFilePath)
        {
            if (newTree.TryFindImmediateChild("project.json", out IProjectTree projectJsonNode))
            {
                return projectJsonNode;
            }

            var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
            if (newTree.TryFindImmediateChild($"{projectName}.project.json", out projectJsonNode))
            {
                return projectJsonNode;
            }

            return null;
        }

        private void RegisterFileWatcherAsync(string projectLockJsonFilePath)
        {
            // Note file change service is free-threaded
            if (_fileChangeService != null && projectLockJsonFilePath != null)
            {
                int hr = _fileChangeService.AdviseFileChange(projectLockJsonFilePath, (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Add | _VSFILECHANGEFLAGS.VSFILECHG_Del), this, out _filechangeCookie);
                ErrorHandler.ThrowOnFailure(hr);
            }

            _fileBeingWatched = projectLockJsonFilePath;
        }

        private void UnregisterFileWatcherIfAny()
        {
            // Note file change service is free-threaded
            if (_filechangeCookie != VSConstants.VSCOOKIE_NIL && _fileChangeService != null)
            {
                // There's nothing for us to do if this fails. So ignore the return value.
                _fileChangeService?.UnadviseFileChange(_filechangeCookie);
            }
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                _treeWatcher.Dispose();
                UnregisterFileWatcherIfAny();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a project.lock.json file changes.
        /// </summary>
        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            // Kick off the operation to notify the project change in a different thread irregardless of
            // the kind of change since we are interested in all changes.
            _projectServices.ThreadingService.Fork(async () =>
            {
                try
                {
                    await _projectServices.Project.Services.ProjectAsynchronousTasks.LoadedProjectAsync(async () =>
                    {
                        using (var access = await _projectLockService.WriteLockAsync())
                        {
                            // notify all the loaded configured projects
                            var currentProjects = _projectServices.Project.LoadedConfiguredProjects;
                            foreach (var configuredProject in currentProjects)
                            {
                                // Inside a write lock, we should get back to the same thread.
                                var project = await access.GetProjectAsync(configuredProject).ConfigureAwait(true);
                                project.MarkDirty();
                                configuredProject.NotifyProjectChange();
                            }
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    // Project is already unloaded
                }
            }, unconfiguredProject: _projectServices.Project,
               factory: _projectServices.ThreadingService.JoinableTaskFactory);

            return VSConstants.S_OK;
        }

        public int DirectoryChanged(string pszDirectory)
        {
            return VSConstants.S_OK;
        }
    }
}
