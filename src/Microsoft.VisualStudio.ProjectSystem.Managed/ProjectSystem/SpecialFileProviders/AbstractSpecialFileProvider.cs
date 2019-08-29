// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides the base class for all <see cref="ISpecialFileProvider"/> instances.
    /// </summary>
    internal abstract class AbstractSpecialFileProvider : ISpecialFileProvider
    {
        private readonly IProjectTreeService _treeService;

        protected AbstractSpecialFileProvider(IProjectTreeService treeService)
        {
            _treeService = treeService;
        }

        public virtual async Task<string?> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags, CancellationToken cancellationToken = default)
        {
            // Make sure at least have a tree before we start searching it
            IProjectTreeServiceState state = await _treeService.PublishAnyNonLoadingTreeAsync(cancellationToken);

            string? path = await FindFileAsync(state.TreeProvider, state.Tree);
            if (path == null)
            {
                // Not found, let's find the default path, and then create it if needed
                path = await GetDefaultFileAsync(state.TreeProvider, state.Tree);

                if (path != null && (flags & SpecialFileFlags.CreateIfNotExist) == SpecialFileFlags.CreateIfNotExist)
                {
                    await CreateFileAsync(path);
                }
            }

            // We always return the default path, regardless of whether we created it or it exists, as per contract
            return path;
        }

        protected abstract Task<string?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root);

        protected abstract Task<string?> GetDefaultFileAsync(IProjectTreeProvider provider, IProjectTree root);

        protected abstract Task CreateFileAsync(string path);
    }
}
