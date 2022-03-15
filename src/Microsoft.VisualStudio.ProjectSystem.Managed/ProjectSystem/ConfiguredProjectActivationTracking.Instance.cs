// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed partial class ConfiguredProjectActivationTracking
    {
        internal sealed class ConfiguredProjectActivationTrackingInstance : OnceInitializedOnceDisposedAsync, IMultiLifetimeInstance
        {
            private readonly ConfiguredProject _project;
            private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;
            private readonly ITargetBlock<IProjectVersionedValue<(IProjectCapabilitiesSnapshot, ConfiguredProject)>> _targetBlock;
            private readonly OrderPrecedenceImportCollection<IActiveConfigurationComponent> _components;

            private IReadOnlyCollection<IActiveConfigurationComponent> _activeComponents = Array.Empty<IActiveConfigurationComponent>();
            private IDisposable? _subscription;

            public ConfiguredProjectActivationTrackingInstance(
                IProjectThreadingService threadingService,
                ConfiguredProject project,
                IActiveConfiguredProjectProvider activeConfiguredProjectProvider,
                OrderPrecedenceImportCollection<IActiveConfigurationComponent> components)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _activeConfiguredProjectProvider = activeConfiguredProjectProvider;
                _targetBlock = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<(IProjectCapabilitiesSnapshot, ConfiguredProject)>>(OnChangeAsync, project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality);
                _components = components;
            }

            public Task InitializeAsync()
            {
                return InitializeAsync(CancellationToken.None);
            }

            protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                _subscription = ProjectDataSources.SyncLinkTo(
                    _project.Capabilities.SourceBlock.SyncLinkOptions(),
                    _activeConfiguredProjectProvider.ActiveConfiguredProjectBlock.SyncLinkOptions(),
                    linkOptions: DataflowOption.PropagateCompletion,
                    target: _targetBlock,
                    cancellationToken: cancellationToken);

                return Task.CompletedTask;
            }

            protected override Task DisposeCoreAsync(bool initialized)
            {
                _subscription?.Dispose();
                _targetBlock.Complete();

                return DeactivateAsync(_activeComponents);
            }

            private async Task OnChangeAsync(IProjectVersionedValue<(IProjectCapabilitiesSnapshot, ConfiguredProject)> e)
            {
                // We'll get called back in main two situations (notwithstanding version-only updates):
                //
                //   - The active configuration changed, and our configuration might
                //     be now active or might no longer be active.
                //
                //   - The capabilities changed in our configuration.
                //
                // In both situations, we may need to activate or deactivate 
                // IActiveConfigurationComponent instances.

                IProjectCapabilitiesSnapshot snapshot = e.Value.Item1;
                bool isActive = e.Value.Item2.ProjectConfiguration.Equals(_project.ProjectConfiguration);

                using var capabilitiesContext = ProjectCapabilitiesContext.CreateIsolatedContext(_project, snapshot);

                // If we're not active, there are no future services to activate
                IReadOnlyCollection<IActiveConfigurationComponent> futureComponents = isActive
                    ? _components.Select(s => s.Value).ToList()
                    : Array.Empty<IActiveConfigurationComponent>();

                var diff = new SetDiff<IActiveConfigurationComponent>(_activeComponents, futureComponents);

                await DeactivateAsync(diff.Removed);
                await ActivateAsync(diff.Added);

                _activeComponents = futureComponents;
            }

            private static Task DeactivateAsync(IEnumerable<IActiveConfigurationComponent> services)
            {
                return Task.WhenAll(services.Select(c => c.DeactivateAsync()));
            }

            private static Task ActivateAsync(IEnumerable<IActiveConfigurationComponent> services)
            {
                return Task.WhenAll(services.Select(c => c.ActivateAsync()));
            }
        }
    }
}
