// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Contains monikers for icons shipped with Managed Project System.
    /// </summary>
    public static class ManagedImageMonikers
    {
        // These GUIDs and IDs are defined in src\Microsoft.VisualStudio.ProjectSystem.Managed.VS\ManagedImages.imagemanifest

        private static readonly Guid s_manifestGuid = new Guid("{259567C1-AA6B-46BF-811C-C145DD9F3B48}");

        public static ImageMoniker ApplicationPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 0 };
        public static ImageMoniker ApplicationWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 1 };
        public static ImageMoniker CodeInformationPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 2 };
        public static ImageMoniker CodeInformationWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 3 };
        public static ImageMoniker Component => new ImageMoniker { Guid = s_manifestGuid, Id = 4 };
        public static ImageMoniker ComponentPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 5 };
        public static ImageMoniker ComponentWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 6 };
        public static ImageMoniker ErrorSmall => new ImageMoniker { Guid = s_manifestGuid, Id = 7 };
        public static ImageMoniker LibraryWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 8 };
        public static ImageMoniker NuGetGrey => new ImageMoniker { Guid = s_manifestGuid, Id = 9 };
        public static ImageMoniker NuGetGreyPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 10 };
        public static ImageMoniker NuGetGreyWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 11 };
        public static ImageMoniker ReferenceGroup => new ImageMoniker { Guid = s_manifestGuid, Id = 12 };
        public static ImageMoniker ReferenceGroupWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 13 };
        public static ImageMoniker ReferencePrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 14 };
        public static ImageMoniker Sdk => new ImageMoniker { Guid = s_manifestGuid, Id = 15 };
        public static ImageMoniker SdkPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 16 };
        public static ImageMoniker SdkWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 17 };
        public static ImageMoniker SharedProject => new ImageMoniker { Guid = s_manifestGuid, Id = 18 };
        public static ImageMoniker SharedProjectPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 19 };
        public static ImageMoniker SharedProjectWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 20 };
        public static ImageMoniker WarningSmall => new ImageMoniker { Guid = s_manifestGuid, Id = 21 };
        public static ImageMoniker Framework => new ImageMoniker { Guid = s_manifestGuid, Id = 22 };
        public static ImageMoniker FrameworkPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 23 };
        public static ImageMoniker FrameworkWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 24 };
        public static ImageMoniker ProjectImports => new ImageMoniker { Guid = s_manifestGuid, Id = 25 };
        public static ImageMoniker TargetFile => new ImageMoniker { Guid = s_manifestGuid, Id = 26 };
        public static ImageMoniker TargetFilePrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 27 };
        public static ImageMoniker PropertiesFolderClosed => new ImageMoniker { Guid = s_manifestGuid, Id = 28 };
        public static ImageMoniker PropertiesFolderOpened => new ImageMoniker { Guid = s_manifestGuid, Id = 29 };

        // These internal fields are provided for convenience

        internal static ImageMoniker Abbreviation => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Abbreviation };
        internal static ImageMoniker AboutBox => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.AboutBox };
        internal static ImageMoniker AbsolutePosition => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.AbsolutePosition };
        public static ImageMoniker Application => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Application };
        internal static ImageMoniker BinaryFile => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.BinaryFile };
        internal static ImageMoniker Blank => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Blank };
        public static ImageMoniker CodeInformation => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.CodeInformation };
        internal static ImageMoniker CSProjectNode => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.CSProjectNode };
        internal static ImageMoniker CSSharedProject => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.CSSharedProject };
        internal static ImageMoniker FSFileNode => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.FSFileNode };
        internal static ImageMoniker FSProjectNode => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.FSProjectNode };
        internal static ImageMoniker FSScript => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.FSScript };
        internal static ImageMoniker FSSignatureFile => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.FSSignatureFile };
        internal static ImageMoniker GlyphDown => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.GlyphDown };
        internal static ImageMoniker GlyphUp => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.GlyphUp };
        public static ImageMoniker Library => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Library };
        internal static ImageMoniker Path => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Path };
        internal static ImageMoniker PathIcon => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.PathIcon };
        internal static ImageMoniker PathListBox => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.PathListBox };
        internal static ImageMoniker PathListBoxItem => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.PathListBoxItem };
        internal static ImageMoniker QuestionMark => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.QuestionMark };
        public static ImageMoniker Reference => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Reference };
        public static ImageMoniker ReferenceWarning => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.ReferenceWarning };
        // NOTE SharedProject is defined in both manifests
//      internal static ImageMoniker SharedProject    => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.SharedProject    };
        internal static ImageMoniker Sound => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Sound };
        internal static ImageMoniker StatusError => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.StatusError };
        internal static ImageMoniker TextFile => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.TextFile };
        internal static ImageMoniker Uninstall => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Uninstall };
        internal static ImageMoniker VBProjectNode => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.VBProjectNode };
        internal static ImageMoniker VBSharedProject => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.VBSharedProject };
    }
}
