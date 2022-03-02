// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides an implementation of <see cref="IActiveWorkspaceProjectContextHost"/> that delegates
    ///     onto the active configuration's <see cref="IWorkspaceProjectContextHost"/>.
    /// </summary>
    [Export(typeof(IActiveWorkspaceProjectContextHost))]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal class ActiveWorkspaceProjectContextHost : IActiveWorkspaceProjectContextHost
    {
        private readonly IActiveConfiguredValue<IWorkspaceProjectContextHost?> _activeHost;
        private readonly IActiveConfiguredProjectProvider _activeConfiguredProjectProvider;
        private readonly IUnconfiguredProjectTasksService _tasksService;

        [ImportingConstructor]
        public ActiveWorkspaceProjectContextHost(
            IActiveConfiguredValue<IWorkspaceProjectContextHost?> activeHost,
            IActiveConfiguredProjectProvider activeConfiguredProjectProvider,
            IUnconfiguredProjectTasksService tasksService)
        {
            _activeHost = activeHost;
            _activeConfiguredProjectProvider = activeConfiguredProjectProvider;
            _tasksService = tasksService;
        }

        public async Task PublishAsync(CancellationToken cancellationToken = default)
        {
            // The active configuration can change multiple times during initialization in cases where we've incorrectly
            // guessed the configuration via our IProjectConfigurationDimensionsProvider3 implementation.
            // Wait until that has been determined before we publish the wrong configuration.
            await _tasksService.PrioritizedProjectLoadedInHost.WithCancellation(cancellationToken);

            while (true)
            {
                CancellationToken activeConfigChangedToken = _activeConfiguredProjectProvider.ConfigurationActiveCancellationToken;

                using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(activeConfigChangedToken, cancellationToken);

                try
                {
                    IWorkspaceProjectContextHost? host = _activeHost.Value;
                    if (host != null)
                    {
                        await host.PublishAsync(tokenSource.Token);
                    }

                    return;
                }
                catch (OperationCanceledException) when (activeConfigChangedToken.IsCancellationRequested)
                {
                }
            }
        }

        public async Task OpenContextForWriteAsync(Func<IWorkspaceProjectContextAccessor, Task> action)
        {
            while (true)
            {
                try
                {
                    IWorkspaceProjectContextHost? host = _activeHost.Value;
                    if (host != null)
                    {
                        await host.OpenContextForWriteAsync(action);
                    }

                    return;
                }
                catch (ActiveProjectConfigurationChangedException)
                {   // Host was unloaded because configuration changed, retry on new config
                }
            }
        }

        public async Task<T?> OpenContextForWriteAsync<T>(Func<IWorkspaceProjectContextAccessor, Task<T>> action)
        {
            while (true)
            {
                try
                {
                    IWorkspaceProjectContextHost? host = _activeHost.Value;
                    if (host != null)
                    {
                        return await host.OpenContextForWriteAsync(action);
                    }

                    return default;
                }
                catch (ActiveProjectConfigurationChangedException)
                {   // Host was unloaded because configuration changed, retry on new config
                }
            }
        }
    }
}
