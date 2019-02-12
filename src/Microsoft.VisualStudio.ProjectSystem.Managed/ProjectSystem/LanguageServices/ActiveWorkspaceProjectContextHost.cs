// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly ActiveConfiguredProject<IWorkspaceProjectContextHost> _activeHost;
        private readonly IUnconfiguredProjectTasksService _tasksService;

        [ImportingConstructor]
        public ActiveWorkspaceProjectContextHost(ActiveConfiguredProject<IWorkspaceProjectContextHost> activeHost, IUnconfiguredProjectTasksService tasksService)
        {
            _activeHost = activeHost;
            _tasksService = tasksService;
        }

        public async Task PublishAsync(CancellationToken cancellationToken = default)
        {
            // The active configuration can change multiple times during initialization in cases where we've incorrectly
            // guessed the configuration via our IProjectConfigurationDimensionsProvider3 implementationt.
            // Wait until that has been determined before we publish the wrong configuration.
            await _tasksService.PrioritizedProjectLoadedInHost.WithCancellation(cancellationToken);

            await _activeHost.Value.PublishAsync(cancellationToken);
        }

        public async Task OpenContextForWriteAsync(Func<IWorkspaceProjectContextAccessor, Task> action)
        {
            while (true)
            {
                try
                {
                    await _activeHost.Value.OpenContextForWriteAsync(action);
                    return;
                }
                catch (ActiveProjectConfigurationChangedException)
                {   // Host was unloaded because configuration changed, retry on new config
                }
            }
        }

        public async Task<T> OpenContextForWriteAsync<T>(Func<IWorkspaceProjectContextAccessor, Task<T>> action)
        {
            while (true)
            {
                try
                {
                    return await _activeHost.Value.OpenContextForWriteAsync(action);
                }
                catch (ActiveProjectConfigurationChangedException)
                {   // Host was unloaded because configuration changed, retry on new config
                }
            }
        }
    }
}
