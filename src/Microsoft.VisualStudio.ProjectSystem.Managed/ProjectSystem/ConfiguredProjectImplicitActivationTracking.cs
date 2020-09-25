// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Responsible for activating and deactiving <see cref="IImplicitlyActiveService"/> instances.
    /// </summary>
    internal class ConfiguredProjectImplicitActivationTracking : OnceInitializedOnceDisposed
    {
        private readonly ConfiguredProject _project;
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
        private readonly ITargetBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> _targetBlock;
        private IDisposable? _subscription;
        private bool _isActive;

        [ImportingConstructor]
        public ConfiguredProjectImplicitActivationTracking(ConfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService)
        {
            _project = project;
            _activeConfigurationGroupService = activeConfigurationGroupService;
            _targetBlock = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>>(OnActiveConfigurationsChanged, project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality);

            ImplicitlyActiveServices = new OrderPrecedenceImportCollection<IImplicitlyActiveService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IImplicitlyActiveService> ImplicitlyActiveServices { get; }

        [ConfiguredProjectAutoLoad]
        [AppliesTo(ProjectCapability.DotNet)]
        public void Load()
        {
            EnsureInitialized();
        }

        public ITargetBlock<IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>>> TargetBlock => _targetBlock;

        protected override void Initialize()
        {
            _subscription = _activeConfigurationGroupService.ActiveConfigurationGroupSource.SourceBlock.LinkTo(
                target: _targetBlock,
                linkOptions: DataflowOption.PropagateCompletion);
        }

        protected override void Dispose(bool disposing)
        {
            _subscription?.Dispose();
            _targetBlock.Complete();
        }

        private Task OnActiveConfigurationsChanged(IProjectVersionedValue<IConfigurationGroup<ProjectConfiguration>> e)
        {
            if (IsDisposing || IsDisposed)
                return Task.CompletedTask;

            bool nowActive = e.Value.Contains(_project.ProjectConfiguration);

            // Are there any changes for my configuration?
            if (nowActive == _isActive)
                return Task.CompletedTask;

            if (nowActive)
            {
                return OnImplicitlyActivated();
            }
            else if (_isActive)
            {
                return OnImplicitlyDeactivated();
            }

            return Task.CompletedTask;
        }

        private Task OnImplicitlyActivated()
        {
            _isActive = true;

            IEnumerable<Task> tasks = ImplicitlyActiveServices.Select(c => c.Value.ActivateAsync());

            return Task.WhenAll(tasks);
        }

        private Task OnImplicitlyDeactivated()
        {
            _isActive = false;

            IEnumerable<Task> tasks = ImplicitlyActiveServices.Select(c => c.Value.DeactivateAsync());

            return Task.WhenAll(tasks);
        }
    }
}
