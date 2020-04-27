// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Backing object for transitive package reference nodes in the dependencies tree.
    /// </summary>
    internal sealed class PackageReferenceItem : RelatableItemBase
    {
        public AssetsFileTarget Target { get; private set; }
        public AssetsFileTargetLibrary Library { get; private set; }

        public PackageReferenceItem(AssetsFileTarget target, AssetsFileTargetLibrary library)
            : base(GetCaption(library))
        {
            Library = library;
            Target = target;
        }

        internal bool TryUpdateState(AssetsFileTarget target, AssetsFileTargetLibrary library)
        {
            if (ReferenceEquals(Target, target) && ReferenceEquals(Library, library))
            {
                return false;
            }

            Target = target;
            Library = library;
            Text = GetCaption(library);
            return true;
        }

        private static string GetCaption(AssetsFileTargetLibrary library) => $"{library.Name} ({library.Version})";

        public override object Identity => Library.Name;

        public override int Priority => AttachedItemPriority.Package;

        public override ImageMoniker IconMoniker => ManagedImageMonikers.NuGetGrey;

        protected override bool TryGetProjectNode(IProjectTree targetRootNode, IRelatableItem item, [NotNullWhen(returnValue: true)] out IProjectTree? projectTree)
        {
            IProjectTree? typeGroupNode = targetRootNode.FindChildWithFlags(DependencyTreeFlags.PackageDependencyGroup);

            projectTree = typeGroupNode?.FindChildWithFlags(ProjectTreeFlags.Create("$ID:" + Library.Name));

            return projectTree != null;
        }

        public override object? GetBrowseObject() => new BrowseObject(this);

        private sealed class BrowseObject : BrowseObjectBase
        {
            private readonly PackageReferenceItem _item;

            public BrowseObject(PackageReferenceItem item) => _item = item;

            public override string GetComponentName() => $"{_item.Library.Name} ({_item.Library.Version})";

            public override string GetClassName() => VSResources.PackageReferenceBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.PackageReferenceNameDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageReferenceNameDescription))]
            public string Name => _item.Library.Name;

            [BrowseObjectDisplayName(nameof(VSResources.PackageReferenceVersionDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageReferenceVersionDescription))]
            public string Version => _item.Library.Version;

            [BrowseObjectDisplayName(nameof(VSResources.PackageReferencePathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageReferencePathDescription))]
            public string? Path => _item.Target.TryResolvePackagePath(_item.Library.Name, _item.Library.Version, out string? fullPath) ? fullPath : null;
        }
    }
}
