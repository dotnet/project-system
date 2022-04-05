// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class ProjectTreeProvider : IProjectTreeProvider
    {
        public IReceivableSourceBlock<IProjectVersionedValue<IProjectTreeSnapshot>> Tree
        {
            get { throw new NotImplementedException(); }
        }

        public IReceivableSourceBlock<IProjectVersionedValue<IProjectTreeSnapshot>> SourceBlock
        {
            get { throw new NotImplementedException(); }
        }

        public NamedIdentity DataSourceKey
        {
            get { throw new NotImplementedException(); }
        }

        public IComparable DataSourceVersion
        {
            get { throw new NotImplementedException(); }
        }

        ISourceBlock<IProjectVersionedValue<object>> IProjectValueDataSource.SourceBlock
        {
            get { throw new NotImplementedException(); }
        }

        public bool CanCopy(IImmutableSet<IProjectTree> nodes, IProjectTree? receiver, bool deleteOriginal = false)
        {
            throw new NotImplementedException();
        }

        public bool CanRemove(IImmutableSet<IProjectTree> nodes, DeleteOptions deleteOptions = DeleteOptions.None)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CanRenameAsync(IProjectTree node)
        {
            throw new NotImplementedException();
        }

        public IProjectTree? FindByPath(IProjectTree root, string path)
        {
            Requires.NotNull(root, nameof(root));
            Requires.NotNullOrEmpty(path, nameof(path));

            foreach (IProjectTree child in root.GetSelfAndDescendentsBreadthFirst())
            {
                if (StringComparer.CurrentCultureIgnoreCase.Equals(child.FilePath, path))
                    return child;
            }

            return null;
        }

        public string? GetAddNewItemDirectory(IProjectTree target)
        {
            if (target.Flags.Contains(ProjectTreeFlags.Common.DisableAddItemFolder))
                return null;

            if (target.IsRoot())
                return string.Empty;

            return Path.Combine(GetAddNewItemDirectory(target.Parent!), target.Caption);
        }

        public string? GetPath(IProjectTree node)
        {
            return node.FilePath;
        }

        public IDisposable Join()
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(IImmutableSet<IProjectTree> nodes, DeleteOptions deleteOptions = DeleteOptions.None)
        {
            foreach (IProjectTree node in nodes)
            {
                node.Parent?.Remove(node);
            }

            return Task.CompletedTask;
        }

        public Task RenameAsync(IProjectTree node, string value)
        {
            throw new NotImplementedException();
        }
    }
}
