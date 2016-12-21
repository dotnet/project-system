// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders
{
    /// <summary>
    ///     Provides a <see cref="ISpecialFileProvider"/> that handles the AppDesigner, 
    ///     called "Properties" in C# and "My Project" in Visual Basic.
    /// </summary>
    [ExportSpecialFileProvider(SpecialFiles.AppDesigner)]
    [AppliesTo(ProjectCapability.AppDesigner)]
    internal class AppDesignerFolderSpecialFileProvider : ISpecialFileProvider
    {
        private readonly Lazy<IPhysicalProjectTree> _projectTree;
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public AppDesignerFolderSpecialFileProvider(Lazy<IPhysicalProjectTree> projectTree, ProjectProperties properties)
        {
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(properties, nameof(properties));

            _projectTree = projectTree;
            _properties = properties;
        }

        public async Task<string> GetFileAsync(SpecialFiles fileId, SpecialFileFlags flags, CancellationToken cancellationToken = default(CancellationToken))
        {
            string path = FindAppDesignerFolder();
            if (path == null)
            {   
                // Not found, let's find the default path and create it if needed
                path = await GetDefaultAppDesignerFolderPathAsync().ConfigureAwait(false);
                
                if (path != null && (flags & SpecialFileFlags.CreateIfNotExist) == SpecialFileFlags.CreateIfNotExist)
                {
                    await _projectTree.Value.TreeStorage.CreateFolderAsync(path)
                                                        .ConfigureAwait(false);
                }
            }

            // We always return the default path, regardless of whether we created it or it exists, as per contract
            return path;
        }

        private string FindAppDesignerFolder()
        {
            IProjectTree root = _projectTree.Value.CurrentTree;

            IProjectTree folder = root.GetSelfAndDescendentsBreadthFirst().FirstOrDefault(child => child.Flags.HasFlag(ProjectTreeFlags.Common.AppDesignerFolder));
            if (folder == null)
                return null;

            return _projectTree.Value.TreeProvider.GetRootedAddNewItemDirectory(folder);
        }

        private async Task<string> GetDefaultAppDesignerFolderPathAsync()
        {
            string rootPath = _projectTree.Value.TreeProvider.GetRootedAddNewItemDirectory(_projectTree.Value.CurrentTree);

            string folderName = await GetDefaultAppDesignerFolderNameAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(folderName))
                return null; // Developer has set the AppDesigner path to empty

            return Path.Combine(rootPath, folderName);
        }

        private async Task<string> GetDefaultAppDesignerFolderNameAsync()
        {
            AppDesigner general = await _properties.GetAppDesignerPropertiesAsync()
                                                   .ConfigureAwait(false);

            return (string)await general.FolderName.GetValueAsync()
                                                   .ConfigureAwait(false);
        }
    }
}
