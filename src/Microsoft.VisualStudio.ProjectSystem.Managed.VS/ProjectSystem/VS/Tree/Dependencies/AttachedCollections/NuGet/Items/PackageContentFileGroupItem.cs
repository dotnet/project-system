// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Backing object for a group of content items within a package within the dependencies tree.
    /// </summary>
    /// <remarks>
    /// Items within this group have type <see cref="PackageContentFileItem"/>.
    /// </remarks>
    internal sealed class PackageContentFileGroupItem : RelatableItemBase
    {
        public AssetsFileTarget Target { get; }
        public AssetsFileTargetLibrary Library { get; }

        public PackageContentFileGroupItem(AssetsFileTarget target, AssetsFileTargetLibrary library)
            : base(VSResources.PackageContentFilesGroupName)
        {
            Target = target;
            Library = library;
        }

        public override object Identity => Library.Name;

        public override int Priority => AttachedItemPriority.ContentFilesGroup;

        public override ImageMoniker IconMoniker => KnownMonikers.PackageFolderClosed;

        public override ImageMoniker ExpandedIconMoniker => KnownMonikers.PackageFolderOpened;
    }
}
