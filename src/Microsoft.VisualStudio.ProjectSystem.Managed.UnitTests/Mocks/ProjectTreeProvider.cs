// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class ProjectTreeProvider : IProjectTreeProvider
    {
        public NamedIdentity DataSourceKey => throw new NotImplementedException();

        public IComparable DataSourceVersion => throw new NotImplementedException();

        public IReceivableSourceBlock<IProjectVersionedValue<IProjectTreeSnapshot>> SourceBlock => throw new NotImplementedException();

        public IReceivableSourceBlock<IProjectVersionedValue<IProjectTreeSnapshot>> Tree => throw new NotImplementedException();

        ISourceBlock<IProjectVersionedValue<object>> IProjectValueDataSource.SourceBlock => throw new NotImplementedException();

        public bool CanCopy(IImmutableSet<IProjectTree> nodes, IProjectTree receiver, bool deleteOriginal = false)
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

        public IProjectTree FindByPath(IProjectTree root, string path)
        {
            throw new NotImplementedException();
        }

        public string GetAddNewItemDirectory(IProjectTree target)
        {
            if (target.IsRoot())
                return string.Empty;

            return Path.Combine(GetAddNewItemDirectory(target.Parent), target.Caption);
        }

        public string GetPath(IProjectTree node)
        {
            return node.FilePath;
        }

        public IDisposable Join()
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(IImmutableSet<IProjectTree> nodes, DeleteOptions deleteOptions = DeleteOptions.None)
        {
            throw new NotImplementedException();
        }

        public Task RenameAsync(IProjectTree node, string value)
        {
            throw new NotImplementedException();
        }
    }
}
