// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Microsoft.VisualStudio.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides the base class for all <see cref="ISpecialFileProvider"/> instances.
    /// </summary>
    internal abstract class AbstractSpecialFileProvider : ISpecialFileProvider
    {
        private readonly IProjectTreeService _treeService;
        private readonly IPhysicalProjectTreeStorage _storage;
        private readonly bool _isFolder;

        protected AbstractSpecialFileProvider(IPhysicalProjectTree projectTree, bool isFolder = false)
        {
            _treeService = projectTree.TreeService;
            _storage = projectTree.TreeStorage;
            _isFolder = isFolder;
        }

        public virtual async Task<string?> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags, CancellationToken cancellationToken = default)
        {
            // Make sure at least have a tree before we start searching it
            IProjectTreeServiceState state = await _treeService.PublishAnyNonLoadingTreeAsync(cancellationToken);

            // Attempt to find an existing file/folder first
            string? path = await FindFileAsync(state.TreeProvider, state.Tree, flags);
            if (path is null)
            {
                // Otherwise, fall back and create it
                path = await CreateDefaultFileAsync(state.TreeProvider, state.Tree, flags);
            }

            return path;
        }

        private async Task<string?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root, SpecialFileFlags flags)
        {
            IProjectTree? node = await FindFileAsync(provider, root);
            if (node is null)
                return null;

            string? path = GetFilePath(provider, node);
            if (path is not null && flags.HasFlag(SpecialFileFlags.CreateIfNotExist))
            {
                // Similar to legacy, we only verify state if we've been asked to create it
                await VerifyStateAsync(node, path);
            }

            return path;
        }

        private async Task<string?> CreateDefaultFileAsync(IProjectTreeProvider provider, IProjectTree root, SpecialFileFlags flags)
        {
            string? path = await GetDefaultFileAsync(provider, root);

            if (path is not null && flags.HasFlag(SpecialFileFlags.CreateIfNotExist))
            {
                await CreateFileAsync(path);
            }

            // We always return the default path, regardless of whether we created it or it exists, as per contract
            return path;
        }

        protected abstract Task<IProjectTree?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root);

        protected abstract Task<string?> GetDefaultFileAsync(IProjectTreeProvider provider, IProjectTree root);

        protected virtual Task CreateFileAsync(string path)
        {
            return _isFolder ? _storage.CreateFolderAsync(path) : _storage.CreateEmptyFileAsync(path);
        }

        private Task AddFileAsync(string path)
        {
            return _isFolder ? _storage.AddFolderAsync(path) : _storage.AddFileAsync(path);
        }

        private Task VerifyStateAsync(IProjectTree node, string path)
        {
            if (_isFolder != node.IsFolder)
                throw new IOException(string.Format(CultureInfo.CurrentCulture, Resources.SpecialFileProvider_FileOrFolderAlreadyExists, node.Caption), Win32Interop.HResultFromWin32(Win32Interop.ERROR_FILE_EXISTS));

            if (!node.Flags.IsIncludedInProject())
            {   // Excluded from project
                return AddFileAsync(path);
            }

            if (node.Flags.IsMissingOnDisk())
            {   // Project includes it, but missing from disk
                return CreateFileAsync(path);
            }

            return Task.CompletedTask;
        }

        private string? GetFilePath(IProjectTreeProvider provider, IProjectTree node)
        {
            return _isFolder ? provider.GetRootedAddNewItemDirectory(node) : provider.GetPath(node);
        }
    }
}
