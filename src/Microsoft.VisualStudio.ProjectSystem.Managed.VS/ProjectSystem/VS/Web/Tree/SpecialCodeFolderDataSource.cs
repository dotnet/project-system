// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks.Dataflow;

using FolderSetProjectValue = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<System.Collections.Immutable.IImmutableSet<string>>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web.Tree
{
    /// <summary>
    ///     Provides a data source that produces ASP.NET special code folders, including well-known "App_Code" 
    ///     and those specified by `codeSubDirectories` in web.config. These are relative to the project directory.
    /// </summary>
    [Export]
    [AppliesTo("AspNet")] // TODO:
    internal class SpecialCodeFolderDataSource : ProjectValueDataSourceBase<IImmutableSet<string>>
    {
        private IBroadcastBlock<FolderSetProjectValue>? _broadcastBlock;
        private IReceivableSourceBlock<FolderSetProjectValue>? _publicBlock;
        private readonly object _lock = new();

        private ImmutableHashSet<string> _folders = Empty.FileSet;
        private int _version;

        [ImportingConstructor]
        public SpecialCodeFolderDataSource(UnconfiguredProject project)
            : base(project.Services, synchronousDisposal: true, registerDataSource: true)
        {
        }

        public override NamedIdentity DataSourceKey { get; } = new NamedIdentity(nameof(SpecialCodeFolderDataSource));

        public override IComparable DataSourceVersion => _version;

        public override IReceivableSourceBlock<FolderSetProjectValue> SourceBlock
        {
            get
            {
                EnsureInitialized();
                return _publicBlock!;
            }
        }

        protected override void Initialize()
        {
#pragma warning disable RS0030 // False positive
            base.Initialize();
#pragma warning restore RS0030

            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<FolderSetProjectValue>(nameFormat: nameof(SpecialCodeFolderDataSource) + " {1}");
            _publicBlock = _broadcastBlock.SafePublicize();

            PublishValue();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _broadcastBlock?.Complete();
            }

            base.Dispose(disposing);
        }

        public void AddCodeFolder(string relativePath)
        {
            Requires.NotNullOrEmpty(relativePath, nameof(relativePath));

            Assumes.True(relativePath.EndsWith("\\", StringComparison.Ordinal));

            PublishValue(folders => folders.Add(relativePath));
        }

        public void RemoveCodeFolder(string relativePath)
        {
            Requires.NotNullOrEmpty(relativePath, nameof(relativePath));

            Assumes.True(relativePath.EndsWith("\\", StringComparison.Ordinal));

            PublishValue(folders => folders.Remove(relativePath));
        }

        private void PublishValue(Func<ImmutableHashSet<string>, ImmutableHashSet<string>>? action = null)
        {
            lock (_lock)
            {
                _folders = action?.Invoke(_folders) ?? _folders;
                _version++;
                _broadcastBlock.Post(new ProjectVersionedValue<IImmutableSet<string>>(
                    _folders,
                    Empty.ProjectValueVersions.Add(DataSourceKey, _version)));
            }
        }
    }
}
