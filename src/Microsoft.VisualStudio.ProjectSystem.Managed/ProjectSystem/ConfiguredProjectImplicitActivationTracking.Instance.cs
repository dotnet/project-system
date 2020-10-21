// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal partial class ConfiguredProjectImplicitActivationTracking
    {
        internal class ConfiguredProjectImplicitActivationTrackingInstance : OnceInitializedOnceDisposedAsync, IMultiLifetimeInstance
        {
            private readonly ConfiguredProject _project;
            private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;
            private readonly ITargetBlock<IProjectVersionedValue<(IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>)>> _targetBlock;
            private readonly OrderPrecedenceImportCollection<IImplicitlyActiveService> _implicitlyActiveServices;

            private IReadOnlyCollection<IImplicitlyActiveService> _activeServices = Array.Empty<IImplicitlyActiveService>();
            private IDisposable? _subscription;

            public ConfiguredProjectImplicitActivationTrackingInstance(
                IProjectThreadingService threadingService,
                ConfiguredProject project,
                IActiveConfigurationGroupService activeConfigurationGroupService,
                OrderPrecedenceImportCollection<IImplicitlyActiveService> implicitlyActiveServices)
                : base(threadingService.JoinableTaskContext)
            {
                _project = project;
                _activeConfigurationGroupService = activeConfigurationGroupService;
                _targetBlock = DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<(IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>)>>(OnActiveConfigurationsChanged, project.UnconfiguredProject, ProjectFaultSeverity.LimitedFunctionality);
                _implicitlyActiveServices = implicitlyActiveServices;
            }

            public ITargetBlock<IProjectVersionedValue<(IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>)>> TargetBlock => _targetBlock;

            public Task InitializeAsync()
            {
                return InitializeAsync(CancellationToken.None);
            }

            protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
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
                _subscription?.Dispose();
                _targetBlock.Complete();

                return DeactivateAsync(_activeServices);
            }

            internal async Task OnActiveConfigurationsChanged(IProjectVersionedValue<ValueTuple<IProjectCapabilitiesSnapshot, IConfigurationGroup<ProjectConfiguration>>> e)
            {
                // We'll get called back in main two situations (notwithstanding version-only updates):
                //
                //   - The active configuration changed, and our configuration might
                //     be now implicitly active or might no longer be implicitly active.
                //
                //   - The capabilities changed in our configuration.
                //
                // In both situations, we may need to activate or deactivate 
                // IImplicitlyActiveService instances.

                IProjectCapabilitiesSnapshot snapshot = e.Value.Item1;
                bool isActive = e.Value.Item2.Contains(_project.ProjectConfiguration);

                using var capabilitiesContext = ProjectCapabilitiesContext.CreateIsolatedContext(_project, snapshot);

                // If we're not active, there are no future services to activate
                IReadOnlyCollection<IImplicitlyActiveService> futureServices = isActive ? _implicitlyActiveServices.Select(s => s.Value).ToList()
                                                                                        : Array.Empty<IImplicitlyActiveService>();

                var diff = new SetDiff<IImplicitlyActiveService>(_activeServices, futureServices);

                await DeactivateAsync(diff.Removed);
                await ActivateAsync(diff.Added);

                _activeServices = futureServices;
            }

            private static Task DeactivateAsync(IEnumerable<IImplicitlyActiveService> services)
            {
                return Task.WhenAll(services.Select(c => c.DeactivateAsync()));
            }

            private static Task ActivateAsync(IEnumerable<IImplicitlyActiveService> services)
            {
                return Task.WhenAll(services.Select(c => c.ActivateAsync()));
            }
        }
    }
}
