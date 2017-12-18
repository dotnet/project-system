// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IActiveConfiguredProjectService))]
    internal class ActiveConfiguredProjectService : OnceInitializedOnceDisposed, IActiveConfiguredProjectService
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
        private readonly ActionBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> _targetBlock;
        private TaskCompletionSource<object> _isActiveCompletionSource = new TaskCompletionSource<object>();
        private IDisposable _subscription;

        public ActiveConfiguredProjectService(ConfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService, [Import(ExportContractNames.Scopes.ConfiguredProject)]IProjectAsynchronousTasksService tasksService)
        {
            _project = project;
            _activeConfigurationGroupService = activeConfigurationGroupService;
            _tasksService = tasksService;
            _targetBlock = new ActionBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>>((Action<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>>)OnActiveConfigurationsChanged);
        }

        public ITargetBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> TargetBlock => _targetBlock;

        public bool IsActive
        {
            get
            {
                EnsureInitialized();

                return _isActiveCompletionSource.Task.Status == TaskStatus.RanToCompletion;
            }
        }

        public Task IsActiveTask
        {
            get
            {
                EnsureInitialized();

                return _isActiveCompletionSource.Task;
            }
        }

        protected override void Initialize()
        {
            _subscription = _activeConfigurationGroupService.ActiveConfigurationGroupSource.SourceBlock.LinkTo(
                target: _targetBlock,
                linkOptions: new DataflowLinkOptions() { PropagateCompletion = true });

            _tasksService.UnloadCancellationToken.Register(RegisterOptions.ExecuteImmediatelyIfAlreadyCanceled, () =>
            {
                // Unloading, notify anyone listening that we're never going to be active
                _isActiveCompletionSource.TrySetCanceled();
            });
        }

        protected override void Dispose(bool disposing)
        {
            _subscription?.Dispose();
            _targetBlock.Complete();

            // Notify anyone listening that we're never going to be active
            _isActiveCompletionSource.TrySetCanceled();
        }

        private void OnActiveConfigurationsChanged(IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>> e)
        {
            if (IsDisposing || IsDisposed)
                return;

            bool nowActive = e.Value.Contains(_project.ProjectConfiguration);
            bool previouslyActive = IsActive;

            // Are there any changes for my configuration?
            if (nowActive == previouslyActive)
                return;

            if (nowActive)
            {
                OnActivated();
            }
            else if (previouslyActive)
            {
                OnDeactivated();
            }
        }

        private void OnActivated()
        {
            _isActiveCompletionSource.TrySetResult(null);
        }

        private void OnDeactivated()
        {
            _isActiveCompletionSource = new TaskCompletionSource<object>();
            Thread.MemoryBarrier();
        }
    }
}
