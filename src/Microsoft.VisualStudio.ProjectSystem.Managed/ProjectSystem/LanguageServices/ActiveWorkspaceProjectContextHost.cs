// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly ActiveConfiguredProject<IConfiguredProjectImplicitActivationTracking> _activeConfiguredProject;

        [ImportingConstructor]
        public ActiveWorkspaceProjectContextHost(ActiveConfiguredProject<IWorkspaceProjectContextHost> activeHost, ActiveConfiguredProject<IConfiguredProjectImplicitActivationTracking> activeConfiguredProject)
        {
            _activeHost = activeHost;
            _activeConfiguredProject = activeConfiguredProject;
        }

        public Task PublishAsync(CancellationToken cancellationToken = default)
        {
            return _activeHost.Value.PublishAsync(cancellationToken);
        }

        public async Task OpenContextForWriteAsync(Func<IWorkspaceProjectContextAccessor, Task> action)
        {
            while (true)
            {
                try
                {
                    await _activeHost.Value.OpenContextForWriteAsync(action);
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
