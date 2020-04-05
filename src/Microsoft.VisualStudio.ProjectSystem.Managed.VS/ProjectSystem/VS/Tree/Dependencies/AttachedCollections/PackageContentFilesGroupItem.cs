// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Backing object for grouping a package's content files within the dependencies tree.
    /// </summary>
    internal sealed class PackageContentFilesGroupItem : AttachedCollectionItemBase, IContainsAttachedItems, IContainedByAttachedItems
    {
        public static PackageContentFilesGroupItem CreateWithContainsItems(IFileIconProvider fileIconProvider, ImmutableArray<AssetsFileTargetLibraryContentFile> contentFiles)
        {
            Requires.NotNull(fileIconProvider, nameof(fileIconProvider));
            Requires.Argument(!contentFiles.IsDefaultOrEmpty, nameof(contentFiles), "May not be default or empty");

            var item = new PackageContentFilesGroupItem();
            item.ContainsAttachedCollectionSource = new ContainsCollectionSource(fileIconProvider, item, contentFiles);
            return item;
        }

        public static PackageContentFilesGroupItem CreateWithContainedByItems(AssetsFileDependenciesSnapshot snapshot, AssetsFileTargetLibrary library, IEnumerable containedByItems)
        {
            Requires.NotNull(snapshot, nameof(snapshot));
            Requires.NotNull(library, nameof(library));
            Requires.NotNull(containedByItems, nameof(containedByItems));

            var item = new PackageContentFilesGroupItem();
            item.ContainedByAttachedCollectionSource = new MaterializedAttachedCollectionSource(item, containedByItems);
            return item;
        }

        private PackageContentFilesGroupItem()
            : base(VSResources.PackageContentFilesGroupName)
        {
        }

        public override int Priority => AttachedItemPriority.ContentFilesGroup;

        public override ImageMoniker IconMoniker => KnownMonikers.PackageFolderClosed;

        public override ImageMoniker ExpandedIconMoniker => KnownMonikers.PackageFolderOpened;

        public IAttachedCollectionSource? ContainsAttachedCollectionSource { get; private set; }

        public IAttachedCollectionSource? ContainedByAttachedCollectionSource { get; private set; }

        private sealed class ContainsCollectionSource : IAttachedCollectionSource
        {
            private readonly IFileIconProvider _fileIconProvider;
            private readonly PackageContentFilesGroupItem _groupItem;
            private readonly ImmutableArray<AssetsFileTargetLibraryContentFile> _contentFiles;
            private IEnumerable? _items;

            public ContainsCollectionSource(IFileIconProvider fileIconProvider, PackageContentFilesGroupItem groupItem, ImmutableArray<AssetsFileTargetLibraryContentFile> contentFiles)
            {
                _fileIconProvider = fileIconProvider;
                _groupItem = groupItem;
                _contentFiles = contentFiles;
            }

            public object? SourceItem => _groupItem;

            public bool HasItems => true;

            public IEnumerable Items => _items ??= _contentFiles.Select(contentFile => new PackageContentFileItem(_fileIconProvider, _groupItem, contentFile)).ToList();
        }
    }
}
