// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.OperationProgress;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed partial class ConfiguredProjectImplicitActivationTracking
    {
        internal sealed class ConfiguredProjectImplicitActivationTrackingInstance : OnceInitializedOnceDisposedAsync, IMultiLifetimeInstance
        {
            private readonly ConfiguredProject _project;
            private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
            private readonly ITargetBlock<IProjectVersionedValue<(IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>)>> _targetBlock;
            private readonly IDataProgressTrackerService _dataProgressTrackerService;
            private readonly OrderPrecedenceImportCollection<IImplicitlyActiveConfigurationComponent> _components;

            private IReadOnlyCollection<IImplicitlyActiveConfigurationComponent> _activeComponents = Array.Empty<IImplicitlyActiveConfigurationComponent>();
            private IDisposable? _subscription;
            private IDataProgressTrackerServiceRegistration? _dataProgressTrackerRegistration;

            public ConfiguredProjectImplicitActivationTrackingInstance(
                IProjectThreadingService threadingService,
                ConfiguredProject project,
                IActiveConfigurationGroupService activeConfigurationGroupService,
                IDataProgressTrackerService dataProgressTrackerService,
                OrderPrecedenceImportCollection<IImplicitlyActiveConfigurationComponent> components)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _activeConfigurationGroupService = activeConfigurationGroupService;
                _targetBlock = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<(IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>)>>(OnChangeAsync, project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality);
                _dataProgressTrackerService = dataProgressTrackerService;
                _components = components;
            }

            public Task InitializeAsync()
            {
                return InitializeAsync(CancellationToken.None);
            }

            protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                _dataProgressTrackerRegistration = _dataProgressTrackerService.RegisterForIntelliSense(this, _project, nameof(ConfiguredProjectImplicitActivationTrackingInstance));

                _subscription = ProjectDataSources.SyncLinkTo(
                    _project.Capabilities.SourceBlock.SyncLinkOptions(),
                    _activeConfigurationGroupService.ActiveConfigurationGroupSource.SourceBlock.SyncLinkOptions(),
                    linkOptions: DataflowOption.PropagateCompletion,
                    target: _targetBlock,
                    cancellationToken: cancellationToken);

                return Task.CompletedTask;
            }

            protected override Task DisposeCoreAsync(bool initialized)
            {
                _dataProgressTrackerRegistration?.Dispose();
                _subscription?.Dispose();
                _targetBlock.Complete();

                return DeactivateAsync(_activeComponents);
            }

            private async Task OnChangeAsync(IProjectVersionedValue<(IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>)> e)
            {
                // We'll get called back in main two situations (notwithstanding version-only updates):
                //
                //   - The active configuration changed, and our configuration might
                //     be now implicitly active or might no longer be implicitly active.
                //
                //   - The capabilities changed in our configuration.
                //
                // In both situations, we may need to activate or deactivate 
                // IImplicitlyActiveConfigurationComponent instances.

                IProjectCapabilitiesSnapshot snapshot = e.Value.Item1;
                bool isActive = e.Value.Item2.Contains(_project.ProjectConfiguration);

                using var capabilitiesContext = ProjectCapabilitiesContext.CreateIsolatedContext(_project, snapshot);

                // If we're not active, there are no future services to activate
                IReadOnlyCollection<IImplicitlyActiveConfigurationComponent> futureComponents = isActive
                    ? _components.Select(s => s.Value).ToList()
                    : Array.Empty<IImplicitlyActiveConfigurationComponent>();

                var diff = new SetDiff<IImplicitlyActiveConfigurationComponent>(_activeComponents, futureComponents);

                await DeactivateAsync(diff.Removed);
                await ActivateAsync(diff.Added);

                _activeComponents = futureComponents;

                _dataProgressTrackerRegistration?.NotifyOutputDataCalculated(e.DataSourceVersions);
            }

            private static Task DeactivateAsync(IEnumerable<IImplicitlyActiveConfigurationComponent> services)
            {
                return Task.WhenAll(services.Select(c => c.DeactivateAsync()));
            }

            private static Task ActivateAsync(IEnumerable<IImplicitlyActiveConfigurationComponent> services)
            {
                return Task.WhenAll(services.Select(c => c.ActivateAsync()));
            }
        }
    }
}
