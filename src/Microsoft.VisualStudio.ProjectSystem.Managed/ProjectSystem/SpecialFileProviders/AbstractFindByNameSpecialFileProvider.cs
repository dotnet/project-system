// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides <see langword="abstract"/> base class for <see cref="ISpecialFileProvider"/> instances
    ///     that find their special file by file name in the root of the project.
    /// </summary>
    internal abstract class AbstractFindByNameSpecialFileProvider : AbstractSpecialFileProvider
    {
        private readonly string _fileName;

        protected AbstractFindByNameSpecialFileProvider(string fileName, IPhysicalProjectTree projectTree)
            : base(projectTree)
        {
            _fileName = fileName;
        }

        protected override Task<IProjectTree?> FindFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            root.TryFindImmediateChild(_fileName, out IProjectTree? node);

            return Task.FromResult<IProjectTree?>(node);
        }

        protected override Task<string?> GetDefaultFileAsync(IProjectTreeProvider provider, IProjectTree root)
        {
            string? projectPath = provider.GetRootedAddNewItemDirectory(root);
            if (projectPath == null)  // Root has DisableAddItem
                return Task.FromResult<string?>(null);

            string path = Path.Combine(projectPath, _fileName);

            return Task.FromResult<string?>(path);
        }

        protected string? GetDefaultFileAsync(string rootPath)
        {
            return Path.Combine(rootPath, _fileName);
        }
    }
}
