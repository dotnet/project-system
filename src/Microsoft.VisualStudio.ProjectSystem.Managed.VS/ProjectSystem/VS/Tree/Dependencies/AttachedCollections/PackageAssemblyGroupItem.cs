// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Backing object for named group of assemblies within the dependencies tree.
    /// </summary>
    internal sealed class PackageAssemblyGroupItem : AttachedCollectionItemBase, IContainsAttachedItems, IContainedByAttachedItems
    {
        private readonly PackageAssemblyGroupType _groupType;

        internal AssetsFileDependenciesSnapshot Snapshot { get; }
        internal AssetsFileTargetLibrary Library { get; }

        public static PackageAssemblyGroupItem CreateWithContainsItems(AssetsFileDependenciesSnapshot snapshot, AssetsFileTargetLibrary library, PackageAssemblyGroupType groupType, ImmutableArray<string> paths)
        {
            Requires.NotNull(snapshot, nameof(snapshot));
            Requires.NotNull(library, nameof(library));
            Requires.Argument(!paths.IsDefaultOrEmpty, nameof(paths), "May not be default or empty");

            var item = new PackageAssemblyGroupItem(groupType, snapshot, library);
            item.ContainsAttachedCollectionSource = new ContainsCollectionSource(item, paths);
            return item;
        }

        public static PackageAssemblyGroupItem CreateWithContainedByItems(AssetsFileDependenciesSnapshot snapshot, AssetsFileTargetLibrary library, PackageAssemblyGroupType groupType, IEnumerable containedByItems)
        {
            Requires.NotNull(snapshot, nameof(snapshot));
            Requires.NotNull(library, nameof(library));
            Requires.NotNull(containedByItems, nameof(containedByItems));

            var item = new PackageAssemblyGroupItem(groupType, snapshot, library);
            item.ContainedByAttachedCollectionSource = new MaterializedAttachedCollectionSource(item, containedByItems);
            return item;
        }

        private PackageAssemblyGroupItem(PackageAssemblyGroupType groupType, AssetsFileDependenciesSnapshot snapshot, AssetsFileTargetLibrary library)
            : base(GetGroupLabel(groupType))
        {
            Requires.NotNull(snapshot, nameof(snapshot));
            Requires.NotNull(library, nameof(library));

            _groupType = groupType;
            Snapshot = snapshot;
            Library = library;
        }

        private static string GetGroupLabel(PackageAssemblyGroupType groupType)
        {
            return groupType switch
            {
                PackageAssemblyGroupType.CompileTime => VSResources.PackageCompileTimeAssemblyGroupName,
                PackageAssemblyGroupType.Framework => VSResources.PackageFrameworkAssemblyGroupName,
                _ => throw new InvalidEnumArgumentException(nameof(groupType), (int)groupType, typeof(PackageAssemblyGroupType))
            };
        }

        public override int Priority => _groupType switch
        {
            PackageAssemblyGroupType.CompileTime => AttachedItemPriority.CompileTimeAssemblyGroup,
            PackageAssemblyGroupType.Framework => AttachedItemPriority.FrameworkAssemblyGroup,
            _ => throw new InvalidEnumArgumentException(nameof(_groupType), (int)_groupType, typeof(PackageAssemblyGroupType))
        };

        public override ImageMoniker IconMoniker => ManagedImageMonikers.ReferenceGroup;

        public IAttachedCollectionSource? ContainsAttachedCollectionSource { get; private set; }

        public IAttachedCollectionSource? ContainedByAttachedCollectionSource { get; private set; }

        private sealed class ContainsCollectionSource : IAttachedCollectionSource
        {
            private readonly PackageAssemblyGroupItem _groupItem;
            private readonly ImmutableArray<string> _paths;
            private IEnumerable? _items;

            public ContainsCollectionSource(PackageAssemblyGroupItem groupItem, ImmutableArray<string> paths)
            {
                _groupItem = groupItem;
                _paths = paths;
            }

            public object? SourceItem => _groupItem;

            public bool HasItems => true;

            public IEnumerable Items => _items ??= _paths.Select(path => new PackageAssemblyItem(path, _groupItem)).ToList();
        }
    }
}
