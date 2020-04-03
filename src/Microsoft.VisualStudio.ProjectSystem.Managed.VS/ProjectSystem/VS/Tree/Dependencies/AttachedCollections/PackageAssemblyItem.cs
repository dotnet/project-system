// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Backing object for an assembly within a library within the dependencies tree.
    /// </summary>
    /// <remarks>
    /// Items of this type are grouped within <see cref="PackageAssemblyGroupItem"/>.
    /// </remarks>
    internal sealed class PackageAssemblyItem : AttachedCollectionItemBase, IContainedByAttachedItems
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

        public IAttachedCollectionSource? ContainedByAttachedCollectionSource => new MaterializedAttachedCollectionSource(this, new[] { _groupItem });

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
                    AssetsFileTargetLibrary library = groupItem.Library;

                    return groupItem.Snapshot.TryResolvePackagePath(library.Name, library.Version, out string? fullPath)
                        ? System.IO.Path.GetFullPath(System.IO.Path.Combine(fullPath, _assembly._path))
                        : null;
                }
            }
        }
    }
}
