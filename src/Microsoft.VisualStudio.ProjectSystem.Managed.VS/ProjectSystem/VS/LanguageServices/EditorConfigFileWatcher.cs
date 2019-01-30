// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Watches for adds and deletes of .editorconfig files *above* the project
    ///     directory and triggers evaluation as needed. Note that adds and deletes of
    ///     files within the project directory directory already trigger evaluation, so
    ///     this type doesn't handle that case.
    /// </summary>
    internal class EditorConfigFileWatcher : OnceInitializedOnceDisposedAsync, IVsFreeThreadedFileChangeEvents2
    {
        private static readonly TimeSpan s_notifyDelay = TimeSpan.FromMilliseconds(100);

        private readonly IActiveConfiguredProjectSubscriptionService _activeConfiguredProjectSubscriptionService;
        private readonly IVsService<IVsAsyncFileChangeEx> _fileChangeService;
        private readonly IProjectTreeProvider _fileSystemTreeProvider;
        private readonly IUnconfiguredProjectCommonServices _projectServices;
        private readonly IUnconfiguredProjectTasksService _projectTasksService;

        private Dictionary<string, uint> _filesBeingWatched;
        private IDisposable _treeWatcher;
        private ITaskDelayScheduler _taskDelayScheduler;
        private CancellationTokenSource _taskDelaySchedulerCancellationTokenSource;

        [ImportingConstructor]
        public EditorConfigFileWatcher(
            IVsService<SVsFileChangeEx, IVsAsyncFileChangeEx> fileChangeService,
            [Import(ContractNames.ProjectTreeProviders.FileSystemDirectoryTree)] IProjectTreeProvider fileSystemTreeProvider,
            IUnconfiguredProjectCommonServices projectServices,
            IUnconfiguredProjectTasksService projectTasksService,
            IActiveConfiguredProjectSubscriptionService activeConfiguredProjectSubscriptionService)
            : base(projectServices.ThreadingService.JoinableTaskContext)
        {
            Requires.NotNull(fileChangeService, nameof(fileChangeService));
            Requires.NotNull(fileSystemTreeProvider, nameof(fileSystemTreeProvider));
            Requires.NotNull(projectServices, nameof(projectServices));
            Requires.NotNull(projectTasksService, nameof(projectTasksService));
            Requires.NotNull(activeConfiguredProjectSubscriptionService, nameof(activeConfiguredProjectSubscriptionService));

            _fileChangeService = fileChangeService;
            _fileSystemTreeProvider = fileSystemTreeProvider;
            _projectServices = projectServices;
            _projectTasksService = projectTasksService;
            _activeConfiguredProjectSubscriptionService = activeConfiguredProjectSubscriptionService;
        }

#pragma warning disable RS0030 // Do not used banned APIs
        [ConfiguredProjectAutoLoad(RequiresUIThread = false)]
#pragma warning restore RS0030 // Do not used banned APIs
        [AppliesTo(ProjectCapability.DotNet)]
        internal void Load()
        {
            var _ = InitializeAsync();
        }

        /// <summary>
        /// Called on changes to the project tree.
        /// </summary>
        internal async Task DataFlow_ChangedAsync(IProjectVersionedValue<Tuple<IProjectTreeSnapshot, IProjectSubscriptionUpdate>> dataFlowUpdate)
        {
            await InitializeAsync();

            IProjectTreeSnapshot treeSnapshot = dataFlowUpdate.Value.Item1;
            IProjectTree newTree = treeSnapshot.Tree;
            if (newTree == null)
            {
                return;
            }

            // If tree changed when we are disposing then ignore the change.
            if (IsDisposing)
            {
                return;
            }

            IProjectSubscriptionUpdate projectUpdate = dataFlowUpdate.Value.Item2;
            SortedSet<string> updatedEditorConfigFilePaths = GetEditorConfigFilePaths(newTree, projectUpdate);

            var filesToStopWatching = _filesBeingWatched.Keys.Where(p => !updatedEditorConfigFilePaths.Contains(p)).ToArray();
            await UnregisterFileWatchersAsync(filesToStopWatching);

            var filesToStartWatching = updatedEditorConfigFilePaths.Where(p => !_filesBeingWatched.TryGetValue(p, out var _)).ToArray();
            await RegisterFileWatchersAsync(filesToStartWatching);
        }

        private async Task UnregisterFileWatchersAsync(string[] filesToStopWatching)
        {
            IVsAsyncFileChangeEx fileChangeService = await _fileChangeService.GetValueAsync();
            foreach (string editorConfigFilePath in filesToStopWatching)
            {
                uint cookie = _filesBeingWatched[editorConfigFilePath];
                if (cookie != VSConstants.VSCOOKIE_NIL)
                {
                    await fileChangeService.UnadviseFileChangeAsync(cookie);
                }

                _filesBeingWatched.Remove(editorConfigFilePath);
            }
        }

        private async Task RegisterFileWatchersAsync(string[] filesToStartWatching)
        {
            IVsAsyncFileChangeEx fileChangeService = await _fileChangeService.GetValueAsync();
            foreach (string editorConfigFilePath in filesToStartWatching)
            {
                _filesBeingWatched[editorConfigFilePath] =
                    await fileChangeService.AdviseFileChangeAsync(
                        editorConfigFilePath,
                        _VSFILECHANGEFLAGS.VSFILECHG_Add | _VSFILECHANGEFLAGS.VSFILECHG_Del,
                        sink: this);
            }
        }

        /// <summary>
        /// Initialize the watcher.
        /// </summary>
        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // Explicitly get back to the thread pool for the rest of this method so we don't tie up the UI thread;
            await TaskScheduler.Default;

            await _projectTasksService.LoadedProjectAsync(() =>
                {
                    _filesBeingWatched = new Dictionary<string, uint>(StringComparers.Paths);
                    _taskDelaySchedulerCancellationTokenSource = CreateLinkedCancellationTokenSource();
                    _taskDelayScheduler = new TaskDelayScheduler(
                        s_notifyDelay,
                        _projectServices.ThreadingService,
                        _taskDelaySchedulerCancellationTokenSource.Token);

                    // The tree source to get changes to the tree so that we can identify *when* the set of .editorconfig files changes.
                    ProjectDataSources.SourceBlockAndLink<IProjectVersionedValue<IProjectTreeSnapshot>> treeSource = _fileSystemTreeProvider.Tree.SyncLinkOptions();

                    // The source to get the actual set of .editorconfig files
                    StandardRuleDataflowLinkOptions sourceLinkOptions = DataflowOption.WithRuleNames(ConfigurationGeneral.SchemaName);

                    ProjectDataSources.SourceBlockAndLink<IProjectVersionedValue<IProjectSubscriptionUpdate>> editorconfigItemsSource = _activeConfiguredProjectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(sourceLinkOptions);
                    ITargetBlock<IProjectVersionedValue<Tuple<IProjectTreeSnapshot, IProjectSubscriptionUpdate>>> target = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<Tuple<IProjectTreeSnapshot, IProjectSubscriptionUpdate>>>(DataFlow_ChangedAsync);

                    // Join the two sources so that we get synchronized versions of the data.
                    _treeWatcher = ProjectDataSources.SyncLinkTo(treeSource, editorconfigItemsSource, target);

                    return Task.CompletedTask;
                });
        }

        private CancellationTokenSource CreateLinkedCancellationTokenSource()
        {
            // we want to cancel when we switch what file is watched, or when the project is unloaded
            if (_projectServices.Project?.Services?.ProjectAsynchronousTasks?.UnloadCancellationToken != null)
            {
                return CancellationTokenSource.CreateLinkedTokenSource(
                    _projectServices.Project.Services.ProjectAsynchronousTasks.UnloadCancellationToken);
            }
            else
            {
                return new CancellationTokenSource();
            }
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                _taskDelaySchedulerCancellationTokenSource.Cancel();
                _taskDelaySchedulerCancellationTokenSource.Dispose();
                _taskDelayScheduler.Dispose();
                _treeWatcher.Dispose();

                var filesBeingWatched = _filesBeingWatched.Keys.ToArray();
                await UnregisterFileWatchersAsync(filesBeingWatched);
            }
        }

        private static SortedSet<string> GetEditorConfigFilePaths(IProjectTree newTree, IProjectSubscriptionUpdate projectUpdate)
        {
            var editorConfigFilePaths = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            string projectFilePath = projectUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, defaultValue: null);
            if (string.IsNullOrEmpty(projectFilePath))
            {
                return editorConfigFilePaths;
            }

            // TODO (tmeschter): if $(DiscoverEditorConfigFiles) is "false", disable all file watchers by returning an empty set.

            string projectDirectory = Path.GetDirectoryName(projectFilePath);
            string directory = Path.GetDirectoryName(projectDirectory);
            while (directory != null)
            {
                string editorConfigFilePath = Path.Combine(directory, ".editorconfig");
                editorConfigFilePaths.Add(editorConfigFilePath);
            }

            return editorConfigFilePaths;
        }

        private async Task HandleFileChangedAsync(CancellationToken cancellationToken)
        {
            try
            {
                TraceUtilities.TraceVerbose("A .editorconfig file has been added or removed. Marking project dirty.");

                cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable RS0030
                await _projectServices.Project.Services.ProjectAsynchronousTasks.LoadedProjectAsync(async () =>
#pragma warning restore RS0030
                {
                    await _projectServices.ProjectAccessor.EnterWriteLockAsync(async (collection, token) =>
                    {
                        // Notify all the loaded configured projects
                        IEnumerable<ConfiguredProject> currentProjects = _projectServices.Project.LoadedConfiguredProjects;
                        foreach (ConfiguredProject configuredProject in currentProjects)
                        {
                            await _projectServices.ProjectAccessor.OpenProjectForWriteAsync(configuredProject, project =>
                            {
                                project.MarkDirty();
                                configuredProject.NotifyProjectChange();
                            }, ProjectCheckoutOption.DoNotCheckout, cancellationToken);
                        }
                    }, cancellationToken);
                });
            }
            catch (OperationCanceledException)
            {
                // Project is already unloaded.
            }
        }

        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            _taskDelayScheduler.ScheduleAsyncTask(HandleFileChangedAsync);

            return VSConstants.S_OK;
        }

        public int DirectoryChanged(string pszDirectory)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int DirectoryChangedEx(string pszDirectory, string pszFile)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int DirectoryChangedEx2(string pszDirectory, uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            return VSConstants.E_NOTIMPL;
        }
    }
}
