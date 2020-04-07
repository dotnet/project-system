// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Backing object for a content file within a package within the dependencies tree.
    /// </summary>
    /// <remarks>
    /// Items of this type are grouped within <see cref="PackageContentFilesGroupItem"/>.
    /// </remarks>
    internal sealed class PackageContentFileItem : AttachedCollectionItemBase, IContainedByAttachedItems
    {
        private readonly IFileIconProvider _fileIconProvider;
        private readonly PackageContentFilesGroupItem _groupItem;
        private readonly AssetsFileTargetLibraryContentFile _contentFile;

        public PackageContentFileItem(IFileIconProvider fileIconProvider, PackageContentFilesGroupItem groupItem, AssetsFileTargetLibraryContentFile contentFile)
            : base(GetProcessedContentFilePath(contentFile.Path))
        {
            _fileIconProvider = fileIconProvider;
            _groupItem = groupItem;
            _contentFile = contentFile;
        }

        private static string GetProcessedContentFilePath(string rawPath)
        {
            // Content file paths always start with "contentFiles/" so remove it from display strings
            const string Prefix = "contentFiles/";
            return rawPath.StartsWith(Prefix) ? rawPath.Substring(Prefix.Length) : rawPath;
        }

        // All siblings are content files, so no prioritization needed (sort alphabetically)
        public override int Priority => 0;

        public override ImageMoniker IconMoniker => _fileIconProvider.GetFileExtensionImageMoniker(Text);

        public override object? GetBrowseObject() => new BrowseObject(this);

        public IAttachedCollectionSource? ContainedByAttachedCollectionSource => new MaterializedAttachedCollectionSource(this, new[] { _groupItem });

        private sealed class BrowseObject : BrowseObjectBase
        {
            private readonly PackageContentFileItem _item;

            public BrowseObject(PackageContentFileItem item) => _item = item;

            public override string GetComponentName() => _item.Text;

            public override string GetClassName() => VSResources.PackageContentFileBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFilePathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFilePathDescription))]
            public string Path => _item._contentFile.Path;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFileOutputPathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFileOutputPathDescription))]
            public string? OutputPath => _item._contentFile.OutputPath;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFilePPOutputPathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFilePPOutputPathDescription))]
            public string? PPOutputPath => _item._contentFile.PPOutputPath;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFileCodeLanguageDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFileCodeLanguageDescription))]
            public string? CodeLanguage => _item._contentFile.CodeLanguage;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFileBuildActionDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFileBuildActionDescription))]
            public string? BuildAction => _item._contentFile.BuildAction;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFileCopyToOutputDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFileCopyToOutputDescription))]
            public bool CopyToOutput => _item._contentFile.CopyToOutput;
        }
    }
}
