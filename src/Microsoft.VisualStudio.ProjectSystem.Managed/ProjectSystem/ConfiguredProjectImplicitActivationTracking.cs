// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides an implementation of <see cref="IConfiguredProjectActivationTracking"/> that is based on the results of <see cref="IActiveConfigurationGroupService"/>.
    /// </summary>
    [Export(typeof(IConfiguredProjectImplicitActivationTracking))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class ConfiguredProjectImplicitActivationTracking : OnceInitializedOnceDisposed, IConfiguredProjectImplicitActivationTracking
    {
        private readonly ConfiguredProject _project;
        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
        private readonly ITargetBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> _targetBlock;
        private TaskCompletionSource<object?> _isImplicitlyActiveSource = new TaskCompletionSource<object?>();
        private IDisposable? _subscription;

        [ImportingConstructor]
        public ConfiguredProjectImplicitActivationTracking(ConfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService, [Import(ExportContractNames.Scopes.ConfiguredProject)]IProjectAsynchronousTasksService tasksService)
        {
            _project = project;
            _activeConfigurationGroupService = activeConfigurationGroupService;
            _tasksService = tasksService;
            _targetBlock = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>>(OnActiveConfigurationsChanged);

            ImplicitlyActiveServices = new OrderPrecedenceImportCollection<IImplicitlyActiveService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IImplicitlyActiveService> ImplicitlyActiveServices
        {
            get;
        }

        [ConfiguredProjectAutoLoad]
        [AppliesTo(ProjectCapability.DotNet)]
        public void Load()
        {
            EnsureInitialized();
        }

        public ITargetBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> TargetBlock => _targetBlock;

        public bool IsImplicitlyActive
        {
            get
            {
                EnsureInitialized();

                return _isImplicitlyActiveSource.Task.Status == TaskStatus.RanToCompletion;
            }
        }

        public Task ImplicitlyActive
        {
            get
            {
                EnsureInitialized();

                return _isImplicitlyActiveSource.Task;
            }
        }

        protected override void Initialize()
        {
            _subscription = _activeConfigurationGroupService.ActiveConfigurationGroupSource.SourceBlock.LinkTo(
                target: _targetBlock,
                linkOptions: DataflowOption.PropagateCompletion);

            _tasksService.UnloadCancellationToken.Register(RegisterOptions.ExecuteImmediatelyIfAlreadyCanceledAndDisposed, () =>
            {
                // Unloading, notify anyone listening that we're never going to be active
                OnCanceled();
            });
        }

        protected override void Dispose(bool disposing)
        {
            _subscription?.Dispose();
            _targetBlock.Complete();

            // Disposed, notify anyone listening that we're never going to be active
            OnCanceled();
        }

        private Task OnActiveConfigurationsChanged(IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>> e)
        {
            if (IsDisposing || IsDisposed)
                return Task.CompletedTask;

            bool nowActive = e.Value.Contains(_project.ProjectConfiguration);
            bool previouslyActive = IsImplicitlyActive;

            // Are there any changes for my configuration?
            if (nowActive == previouslyActive)
                return Task.CompletedTask;

            if (nowActive)
            {
                return OnImplicitlyActivated();
            }
            else if (previouslyActive)
            {
                return OnImplicitlyDeactivated();
            }

            return Task.CompletedTask;
        }

        private Task OnImplicitlyActivated()
        {
            _isImplicitlyActiveSource.TrySetResult(null);

            IEnumerable<Task> tasks = ImplicitlyActiveServices.Select(c => c.Value.ActivateAsync());

            return Task.WhenAll(tasks);
        }

        private Task OnImplicitlyDeactivated()
        {
            var source = new TaskCompletionSource<object?>();

            // Make sure the writes in constructor don't 
            // move to after we publish the value
            Thread.MemoryBarrier();

            _isImplicitlyActiveSource = source;

            IEnumerable<Task> tasks = ImplicitlyActiveServices.Select(c => c.Value.DeactivateAsync());

            return Task.WhenAll(tasks);
        }

        private void OnCanceled()
        {
            // Notify anyone listening that we're never going to be active
            _isImplicitlyActiveSource.TrySetCanceled();
        }
    }
}
