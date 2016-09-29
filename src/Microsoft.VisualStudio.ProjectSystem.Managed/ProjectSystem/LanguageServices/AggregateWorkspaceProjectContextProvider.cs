// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Provides an <see cref="AggregateWorkspaceProjectContext"/> instance containing set of <see cref="IWorkspaceProjectContext"/>s per-target framework for the given cross targeting project.
    /// </summary>
    internal sealed class AggregateWorkspaceProjectContextProvider
    {
        private readonly object _gate = new object();
        private readonly Lazy<IProjectContextProvider> _contextProvider;

        // Cache the last queried TargetFrameworks and associated AggregateWorkspaceProjectContext.
        // Read/writes for both these fields must be done within a lock to keep them in sync.
        private string _cachedTargetFrameworks;
        private AggregateWorkspaceProjectContext _cachedProjectContext;

        public AggregateWorkspaceProjectContextProvider(Lazy<IProjectContextProvider> contextProvider)
        {
            Requires.NotNull(contextProvider, nameof(contextProvider));

            _contextProvider = contextProvider;
        }

        /// <summary>
        /// Updates the underlying <see cref="AggregateWorkspaceProjectContext"/> for the given target frameworks.
        /// Invoke this API when the inner set of <see cref="IWorkspaceProjectContext"/> for the aggregate contexts need to be recomputed - for example, when the "TargetFrameworks" project property has changed.
        /// </summary>
        public async Task<AggregateWorkspaceProjectContext> UpdateProjectContextAsync(string targetFrameworks, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AggregateWorkspaceProjectContext projectContext;
            lock (_gate)
            {
                if (string.Equals(targetFrameworks, _cachedTargetFrameworks, StringComparison.OrdinalIgnoreCase))
                {
                    return _cachedProjectContext;
                }

                projectContext = _cachedProjectContext;

                // Clear the old cached target frameworks and context as we are going to release the project context.
                _cachedProjectContext = null;
                _cachedTargetFrameworks = null;
            }

            await ReleaseProjectContextAsync(projectContext, _contextProvider).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            projectContext = await _contextProvider.Value.CreateProjectContextAsync().ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            lock (_gate)
            {
                // Check the latest target frameworks again, as another thread might have updated it.
                if (!string.Equals(targetFrameworks, _cachedTargetFrameworks, StringComparison.OrdinalIgnoreCase))
                {
                    _cachedProjectContext = projectContext;
                    _cachedTargetFrameworks = targetFrameworks;
                }

                return _cachedProjectContext;
            }
        }

        /// <summary>
        /// Releases the underlying <see cref="AggregateWorkspaceProjectContext"/>.
        /// </summary>
        public async Task DisposeAsync()
        {
            AggregateWorkspaceProjectContext projectContext;
            lock (_gate)
            {
                projectContext = _cachedProjectContext;
                _cachedProjectContext = null;
                _cachedTargetFrameworks = null;
            }

            await ReleaseProjectContextAsync(projectContext, _contextProvider).ConfigureAwait(false);
        }

        private static async Task ReleaseProjectContextAsync(AggregateWorkspaceProjectContext projectContextOpt, Lazy<IProjectContextProvider> contextProvider)
        {
            if (projectContextOpt != null)
            {
                await contextProvider.Value.ReleaseProjectContextAsync(projectContextOpt).ConfigureAwait(false);
            }
        }
    }
}
