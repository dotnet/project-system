// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Backing object for transitive package reference nodes in the dependencies tree.
    /// </summary>
    internal sealed class PackageReferenceItem : AttachedCollectionItemBase, IContainsAttachedItems, IContainedByAttachedItems
    {
        private readonly AssetsFileTargetLibrary _library;
        private readonly AssetsFileDependenciesSnapshot _snapshot;

        public static PackageReferenceItem CreateWithContainsItems(AssetsFileDependenciesSnapshot snapshot, AssetsFileTargetLibrary library, string? target, IFileIconProvider fileIconProvider)
        {
            var item = new PackageReferenceItem(library, snapshot);
            item.ContainsAttachedCollectionSource = new ContainsCollectionSource(item, snapshot, library, target, fileIconProvider);
            return item;
        }

        public static PackageReferenceItem CreateWithContainedByItems(AssetsFileDependenciesSnapshot snapshot, AssetsFileTargetLibrary library, IEnumerable items)
        {
            var item = new PackageReferenceItem(library, snapshot);
            item.ContainedByAttachedCollectionSource = new MaterializedAttachedCollectionSource(item, items);
            return item;
        }

        private PackageReferenceItem(AssetsFileTargetLibrary library, AssetsFileDependenciesSnapshot snapshot)
            : base($"{library.Name} ({library.Version})")
        {
            _library = library;
            _snapshot = snapshot;
        }

        public override int Priority => AttachedItemPriority.Package;

        public override ImageMoniker IconMoniker => ManagedImageMonikers.NuGetGrey;

        public override object? GetBrowseObject() => new BrowseObject(_library, _snapshot);

        public IAttachedCollectionSource? ContainsAttachedCollectionSource { get; private set; }

        public IAttachedCollectionSource? ContainedByAttachedCollectionSource { get; private set; }

        private sealed class ContainsCollectionSource : IAttachedCollectionSource
        {
            private readonly AssetsFileDependenciesSnapshot _snapshot;
            private readonly AssetsFileTargetLibrary _library;
            private readonly string? _target;
            private readonly IFileIconProvider _fileIconProvider;
            private IEnumerable? _items;

            public ContainsCollectionSource(PackageReferenceItem sourceItem, AssetsFileDependenciesSnapshot snapshot, AssetsFileTargetLibrary library, string? target, IFileIconProvider fileIconProvider)
            {
                SourceItem = sourceItem;
                _snapshot = snapshot;
                _library = library;
                _target = target;
                _fileIconProvider = fileIconProvider;
            }

            public object? SourceItem { get; }

            public bool HasItems => !_library.Dependencies.IsEmpty || !_library.CompileTimeAssemblies.IsEmpty || !_library.ContentFiles.IsEmpty;

            public IEnumerable Items
            {
                get
                {
                    // NOTE any change to the construction of these items must be reflected in the implementation of HasItems
                    if (_items == null)
                    {
                        if (_snapshot.TryGetDependencies(_library.Name, _library.Version, _target, out ImmutableArray<AssetsFileTargetLibrary> dependencies))
                        {
                            int length = _library.CompileTimeAssemblies.IsEmpty ? dependencies.Length : dependencies.Length + 1;

                            ImmutableArray<object>.Builder builder = ImmutableArray.CreateBuilder<object>(length);
                            builder.AddRange(dependencies.Select(dep => CreateWithContainsItems(_snapshot, dep, _target, _fileIconProvider)));

                            if (!_library.CompileTimeAssemblies.IsEmpty)
                            {
                                builder.Add(PackageAssemblyGroupItem.CreateWithContainsItems(_snapshot, _library, PackageAssemblyGroupType.CompileTime, _library.CompileTimeAssemblies));
                            }

                            if (!_library.ContentFiles.IsEmpty)
                            {
                                builder.Add(PackageContentFilesGroupItem.CreateWithContainsItems(_fileIconProvider, _library.ContentFiles));
                            }

                            _items = builder.MoveToImmutable();
                        }
                        else
                        {
                            _items = Enumerable.Empty<object>();
                        }
                    }

                    return _items;
                }
            }
        }

        private sealed class BrowseObject : BrowseObjectBase
        {
            private readonly AssetsFileTargetLibrary _library;
            private readonly AssetsFileDependenciesSnapshot _snapshot;

            public BrowseObject(AssetsFileTargetLibrary library, AssetsFileDependenciesSnapshot snapshot)
            {
                _library = library;
                _snapshot = snapshot;
            }

            public override string GetComponentName() => $"{_library.Name} ({_library.Version})";

            public override string GetClassName() => VSResources.PackageReferenceBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.PackageReferenceNameDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageReferenceNameDescription))]
            public string Name => _library.Name;

            [BrowseObjectDisplayName(nameof(VSResources.PackageReferenceVersionDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageReferenceVersionDescription))]
            public string Version => _library.Version;

            [BrowseObjectDisplayName(nameof(VSResources.PackageReferencePathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageReferencePathDescription))]
            public string? Path => _snapshot.TryResolvePackagePath(_library.Name, _library.Version, out string? fullPath) ? fullPath : null;
        }
    }
}
