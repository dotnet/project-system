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
        private readonly ITargetBlock<IProjectVersionedValue<(IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>)>> _targetBlock;
        private IReadOnlyCollection<IImplicitlyActiveService> _activeServices = Array.Empty<IImplicitlyActiveService>();
        private IDisposable? _subscription;

        [ImportingConstructor]
        public ConfiguredProjectImplicitActivationTracking(ConfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService)
        {
            _project = project;
            _activeConfigurationGroupService = activeConfigurationGroupService;
            _targetBlock = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<(IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>)>>(OnActiveConfigurationsChanged, project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality);

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

        public ITargetBlock<IProjectVersionedValue<(IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>)>> TargetBlock => _targetBlock;

        protected override void Initialize()
        {
            _subscription = ProjectDataSources.SyncLinkTo(
                        _project.Capabilities.SourceBlock.SyncLinkOptions(),
                        _activeConfigurationGroupService.ActiveConfigurationGroupSource.SourceBlock.SyncLinkOptions(),
                        linkOptions: DataflowOption.PropagateCompletion,
                        target: _targetBlock);
        }

        protected override void Dispose(bool disposing)
        {
            _subscription?.Dispose();
            _targetBlock.Complete();
        }

        private async Task OnActiveConfigurationsChanged(IProjectVersionedValue<ValueTuple<IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>>> e)
        {
            using var capabilitiesContext = ProjectCapabilitiesContext.CreateIsolatedContext(_project, e.Value.Item1);

            bool isActive = e.Value.Item2.Contains(_project.ProjectConfiguration);

            // If we're not active, there are no future services to activate
            IReadOnlyCollection<IImplicitlyActiveService> futureServices = isActive ? ImplicitlyActiveServices.Select(s => s.Value).ToList()
                                                                                    : Array.Empty<IImplicitlyActiveService>();

            // Deactive currently "active" services that no longer applicable
            IEnumerable<IImplicitlyActiveService> servicesToDeactivate = _activeServices.Except(futureServices);
            await Task.WhenAll(servicesToDeactivate.Select(c => c.DeactivateAsync()));

            // Activate "non-active" services that are now applicable
            IEnumerable<IImplicitlyActiveService> servicesToActivate = futureServices.Except(_activeServices);
            await Task.WhenAll(servicesToActivate.Select(c => c.ActivateAsync()));

            _activeServices = futureServices;
        }
    }
}
