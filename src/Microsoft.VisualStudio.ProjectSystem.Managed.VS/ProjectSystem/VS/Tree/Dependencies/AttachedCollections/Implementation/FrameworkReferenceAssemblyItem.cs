// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections.Implementation;

internal sealed class FrameworkReferenceAssemblyItem : RelatableItemBase, IObjectBrowserItem
{
    private const int IDM_VS_CTXT_TRANSITIVE_ASSEMBLY_REFERENCE = 0x04B1;

    private static readonly IContextMenuController s_defaultMenuController = CreateContextMenuController(VsMenus.guidSHLMainMenu, IDM_VS_CTXT_TRANSITIVE_ASSEMBLY_REFERENCE);

    public string AssemblyName { get; }
    public string? Path { get; }
    public string? AssemblyVersion { get; }
    public string? FileVersion { get; }
    public FrameworkReferenceIdentity Framework { get; }

    public FrameworkReferenceAssemblyItem(string assemblyName, string? path, string? assemblyVersion, string? fileVersion, FrameworkReferenceIdentity framework)
        : base(assemblyName)
    {
        Requires.NotNull(framework);
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

    string? IObjectBrowserItem.AssemblyPath => GetAssemblyPath();

    private string? GetAssemblyPath() => Path is not null
        ? Microsoft.IO.Path.GetFullPath(Microsoft.IO.Path.Combine(Framework.Path, Path))
        : null;

    private sealed class BrowseObject(FrameworkReferenceAssemblyItem item) : LocalizableProperties
    {
        public override string GetComponentName() => item.AssemblyName;

        public override string GetClassName() => VSResources.FrameworkAssemblyBrowseObjectClassName;

        [BrowseObjectDisplayName(nameof(VSResources.FrameworkAssemblyAssemblyNameDisplayName))]
        [BrowseObjectDescription(nameof(VSResources.FrameworkAssemblyAssemblyNameDescription))]
        public string AssemblyName => item.Text;

        [BrowseObjectDisplayName(nameof(VSResources.FrameworkAssemblyPathDisplayName))]
        [BrowseObjectDescription(nameof(VSResources.FrameworkAssemblyPathDescription))]
        public string Path => item.GetAssemblyPath() ?? "";

        [BrowseObjectDisplayName(nameof(VSResources.FrameworkAssemblyAssemblyVersionDisplayName))]
        [BrowseObjectDescription(nameof(VSResources.FrameworkAssemblyAssemblyVersionDescription))]
        public string AssemblyVersion => item.AssemblyVersion ?? "";

        [BrowseObjectDisplayName(nameof(VSResources.FrameworkAssemblyFileVersionDisplayName))]
        [BrowseObjectDescription(nameof(VSResources.FrameworkAssemblyFileVersionDescription))]
        public string FileVersion => item.FileVersion ?? "";
    }
}
