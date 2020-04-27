// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Backing object for content files within a package within the dependencies tree.
    /// </summary>
    /// <remarks>
    /// Items of this type are grouped within <see cref="PackageContentFileGroupItem"/>.
    /// </remarks>
    internal sealed class PackageContentFileItem : RelatableItemBase
    {
        public AssetsFileTarget Target { get; }
        public AssetsFileTargetLibrary Library { get; }
        public AssetsFileTargetLibraryContentFile ContentFile { get; }

        private readonly IFileIconProvider _fileIconProvider;

        public PackageContentFileItem(AssetsFileTarget target, AssetsFileTargetLibrary library, AssetsFileTargetLibraryContentFile contentFile, IFileIconProvider fileIconProvider)
            : base(GetProcessedContentFilePath(contentFile.Path))
        {
            Target = target;
            Library = library;
            ContentFile = contentFile;
            _fileIconProvider = fileIconProvider;
        }

        private static string GetProcessedContentFilePath(string rawPath)
        {
            // Content file paths always start with "contentFiles/" so remove it from display strings
            const string Prefix = "contentFiles/";
            return rawPath.StartsWith(Prefix) ? rawPath.Substring(Prefix.Length) : rawPath;
        }

        public override object Identity => Tuple.Create(Library.Name, ContentFile.Path);

        // All siblings are content files, so no prioritization needed (sort alphabetically)
        public override int Priority => 0;

        public override ImageMoniker IconMoniker => _fileIconProvider.GetFileExtensionImageMoniker(Text);

        public override object? GetBrowseObject() => new BrowseObject(this);

        private sealed class BrowseObject : BrowseObjectBase
        {
            private readonly PackageContentFileItem _item;

            public BrowseObject(PackageContentFileItem item) => _item = item;

            public override string GetComponentName() => _item.Text;

            public override string GetClassName() => VSResources.PackageContentFileBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFilePathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFilePathDescription))]
            public string Path => _item.ContentFile.Path;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFileOutputPathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFileOutputPathDescription))]
            public string? OutputPath => _item.ContentFile.OutputPath;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFilePPOutputPathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFilePPOutputPathDescription))]
            public string? PPOutputPath => _item.ContentFile.PPOutputPath;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFileCodeLanguageDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFileCodeLanguageDescription))]
            public string? CodeLanguage => _item.ContentFile.CodeLanguage;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFileBuildActionDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFileBuildActionDescription))]
            public string? BuildAction => _item.ContentFile.BuildAction;

            [BrowseObjectDisplayName(nameof(VSResources.PackageContentFileCopyToOutputDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageContentFileCopyToOutputDescription))]
            public bool CopyToOutput => _item.ContentFile.CopyToOutput;
        }
    }
}
