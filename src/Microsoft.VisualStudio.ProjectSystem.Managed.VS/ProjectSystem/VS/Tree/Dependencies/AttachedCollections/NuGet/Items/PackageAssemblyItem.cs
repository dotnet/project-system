// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Backing object for an assembly within a library within the dependencies tree.
    /// </summary>
    /// <remarks>
    /// Items of this type are grouped within <see cref="PackageAssemblyGroupItem"/>.
    /// </remarks>
    internal sealed class PackageAssemblyItem : RelatableItemBase
    {
        public AssetsFileTarget Target { get; }
        public AssetsFileTargetLibrary Library { get; }
        public string Path { get; }
        public PackageAssemblyGroupType GroupType { get; }

        public PackageAssemblyItem(AssetsFileTarget target, AssetsFileTargetLibrary library, string path, PackageAssemblyGroupType groupType)
            : base(System.IO.Path.GetFileName(path))
        {
            Target = target;
            Library = library;
            Path = path;
            GroupType = groupType;
        }

        public override object Identity => Tuple.Create(Library.Name, Path);

        // All siblings are assemblies, so no prioritization needed (sort alphabetically)
        public override int Priority => 0;

        public override ImageMoniker IconMoniker => KnownMonikers.Reference;

        public override object? GetBrowseObject() => new BrowseObject(this);

        private sealed class BrowseObject : BrowseObjectBase
        {
            private readonly PackageAssemblyItem _item;

            public BrowseObject(PackageAssemblyItem library) => _item = library;

            public override string GetComponentName() => _item.Text;

            public override string GetClassName() => VSResources.PackageAssemblyBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.PackageAssemblyNameDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageAssemblyNameDescription))]
            public string Name => _item.Text;

            [BrowseObjectDisplayName(nameof(VSResources.PackageAssemblyPathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageAssemblyPathDescription))]
            public string? Path
            {
                get
                {
                    return _item.Target.TryResolvePackagePath(_item.Library.Name, _item.Library.Version, out string? fullPath)
                        ? System.IO.Path.GetFullPath(System.IO.Path.Combine(fullPath, _item.Path))
                        : null;
                }
            }
        }
    }
}
