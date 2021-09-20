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

        public Task PublishAsync(CancellationToken cancellationToken = default)
        {
            return DoWithConfigurationChangeProtectionAsync<object?>(
                static async (host, token) =>
                {
                    await host.PublishAsync(token);
                    return null;
                },
                cancellationToken);
        }

        public Task OpenContextForWriteAsync(Func<IWorkspaceProjectContextAccessor, Task> action)
        {
            return DoWithConfigurationChangeProtectionAsync<object?>(
                async (host, token) =>
                {
                    await host.OpenContextForWriteAsync(action).WithCancellation(token);
                    return null;
                });
        }

        public Task<T?> OpenContextForWriteAsync<T>(Func<IWorkspaceProjectContextAccessor, Task<T>> action)
        {
            return DoWithConfigurationChangeProtectionAsync(
                async (host, token) => await host.OpenContextForWriteAsync(action).WithCancellation(token));
        }

        /// <summary>
        /// Ensures the project is initialized and that the active configuration remains active throughout
        /// an async operation. If the active configuration changes before <paramref name="func"/> completes,
        /// it is cancelled and invoked again with the newly active configuration.
        /// </summary>
        private async Task<T?> DoWithConfigurationChangeProtectionAsync<T>(
            Func<IWorkspaceProjectContextHost, CancellationToken, Task<T>> func,
            CancellationToken cancellationToken = default)
        {
            // The active configuration can change multiple times during initialization in cases where we've incorrectly
            // guessed the configuration via our IProjectConfigurationDimensionsProvider3 implementation.
            // Wait until that has been determined before we publish the wrong configuration.
            await _tasksService.PrioritizedProjectLoadedInHost.WithCancellation(cancellationToken);

            while (true)
            {
                IWorkspaceProjectContextHost? host = _activeHost.Value;

                if (host is null)
                {
                    // No configuration is active
                    return default;
                }

                CancellationToken activeConfigChangedToken = _activeConfiguredProjectProvider.ConfigurationActiveCancellationToken;

                using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(activeConfigChangedToken, cancellationToken);

                try
                {
                    return await func(host, tokenSource.Token);
                }
                catch (OperationCanceledException) when (activeConfigChangedToken.IsCancellationRequested)
                {
                    // Host was unloaded because configuration changed, retry on new config
                }
            }
        }
    }
}
