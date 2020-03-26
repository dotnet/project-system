// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Backing object for named group of assemblies within the dependencies tree.
    /// </summary>
    internal sealed class PackageAssemblyGroupItem : AttachedCollectionItemBase, IContainsAttachedItems, IAttachedCollectionSource
    {
        private readonly PackageAssemblyGroupType _groupType;
        private readonly ImmutableArray<string> _paths;
        private readonly AssetsFileDependenciesSnapshot _snapshot;
        private readonly AssetsFileTargetLibrary _library;
        private IEnumerable? _items;

        public PackageAssemblyGroupItem(PackageAssemblyGroupType groupType, ImmutableArray<string> paths, AssetsFileDependenciesSnapshot snapshot, AssetsFileTargetLibrary library)
            : base(GetGroupLabel(groupType))
        {
            Requires.Argument(!paths.IsEmpty, nameof(paths), "May not be empty");
            Requires.NotNull(snapshot, nameof(snapshot));
            Requires.NotNull(library, nameof(library));

            _groupType = groupType;
            _paths = paths;
            _snapshot = snapshot;
            _library = library;
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

        public override ImageMoniker ExpandedIconMoniker => ManagedImageMonikers.ReferenceGroup;

        public override object? GetBrowseObject() => null;

        public object? SourceItem => null;
        
        public bool HasItems => true;
        
        public IEnumerable Items => _items ??= _paths.Select(path => new PackageAssemblyItem(path, this)).ToList();

        public IAttachedCollectionSource ContainsAttachedCollectionSource => this;

        private sealed class PackageAssemblyItem : AttachedCollectionItemBase
        {
            private readonly string _path;
            private readonly PackageAssemblyGroupItem _groupItem;

            public PackageAssemblyItem(string path, PackageAssemblyGroupItem groupItem)
                : base(Path.GetFileName(path))
            {
                _path = path;
                _groupItem = groupItem;
            }

            // All siblings are assemblies, so no prioritization needed (sort alphabetically)
            public override int Priority => 0;

            public override ImageMoniker IconMoniker => KnownMonikers.Reference;

            public override object? GetBrowseObject() => new BrowseObject(this);

            private sealed class BrowseObject : BrowseObjectBase
            {
                private readonly PackageAssemblyItem _assembly;

                public BrowseObject(PackageAssemblyItem library) => _assembly = library;

                public override string GetComponentName() => _assembly.Text;

                public override string GetClassName() => VSResources.PackageAssemblyBrowseObjectClassName;

                [BrowseObjectDisplayName(nameof(VSResources.PackageAssemblyNameDisplayName))]
                [BrowseObjectDescription(nameof(VSResources.PackageAssemblyNameDescription))]
                public string Name => _assembly.Text;

                [BrowseObjectDisplayName(nameof(VSResources.PackageAssemblyPathDisplayName))]
                [BrowseObjectDescription(nameof(VSResources.PackageAssemblyPathDescription))]
                public string? Path
                {
                    get
                    {
                        PackageAssemblyGroupItem groupItem = _assembly._groupItem;
                        AssetsFileTargetLibrary library = groupItem._library;

                        return groupItem._snapshot.TryResolvePackagePath(library.Name, library.Version, out string? fullPath)
                            ? System.IO.Path.GetFullPath(System.IO.Path.Combine(fullPath, _assembly._path))
                            : null;
                    }
                }
            }
        }
    }
}
