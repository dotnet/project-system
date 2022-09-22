// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections.Implementation
{
    internal sealed class FrameworkReferenceAssemblyItem : RelatableItemBase
    {
        private const int IDM_VS_CTXT_TRANSITIVE_ASSEMBLY_REFERENCE = 0x04B1;

        private static readonly IContextMenuController s_defaultMenuController = new MenuController(VsMenus.guidSHLMainMenu, IDM_VS_CTXT_TRANSITIVE_ASSEMBLY_REFERENCE);

        public string AssemblyName { get; }
        public string? Path { get; }
        public string? AssemblyVersion { get; }
        public string? FileVersion { get; }
        public FrameworkReferenceIdentity Framework { get; }

        public FrameworkReferenceAssemblyItem(string assemblyName, string? path, string? assemblyVersion, string? fileVersion, FrameworkReferenceIdentity framework)
            : base(assemblyName)
        {
            Requires.NotNull(framework, nameof(framework));
            AssemblyName = assemblyName;
            Path = path;
            AssemblyVersion = assemblyVersion;
            FileVersion = fileVersion;
            Framework = framework;
        }

        public override object Identity => Text;
        public override int Priority => 0;
        public override ImageMoniker IconMoniker => KnownMonikers.ReferencePrivate;

        protected override IContextMenuController? ContextMenuController => s_defaultMenuController;

        public override object? GetBrowseObject() => new BrowseObject(this);

        private sealed class BrowseObject : LocalizableProperties
        {
            private readonly FrameworkReferenceAssemblyItem _item;

            public BrowseObject(FrameworkReferenceAssemblyItem log) => _item = log;

            public override string GetComponentName() => _item.AssemblyName;

            public override string GetClassName() => VSResources.FrameworkAssemblyBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.FrameworkAssemblyAssemblyNameDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.FrameworkAssemblyAssemblyNameDescription))]
            public string AssemblyName => _item.Text;

            [BrowseObjectDisplayName(nameof(VSResources.FrameworkAssemblyPathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.FrameworkAssemblyPathDescription))]
            public string Path => _item.Path is not null
                ? System.IO.Path.GetFullPath(System.IO.Path.Combine(_item.Framework.Path, _item.Path))
                : "";

            [BrowseObjectDisplayName(nameof(VSResources.FrameworkAssemblyAssemblyVersionDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.FrameworkAssemblyAssemblyVersionDescription))]
            public string AssemblyVersion => _item.AssemblyVersion ?? "";

            [BrowseObjectDisplayName(nameof(VSResources.FrameworkAssemblyFileVersionDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.FrameworkAssemblyFileVersionDescription))]
            public string FileVersion => _item.FileVersion ?? "";
        }
    }
}
