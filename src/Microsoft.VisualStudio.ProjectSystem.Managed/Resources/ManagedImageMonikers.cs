// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem;

[assembly: DebuggerDisplay("{Microsoft.VisualStudio.ProjectSystem.VS.ManagedImageMonikers.ImageMonikerDebugDisplay(this)}", Target = typeof(ImageMoniker))]
[assembly: DebuggerDisplay("{Microsoft.VisualStudio.ProjectSystem.VS.ManagedImageMonikers.ProjectImageMonikerDebugDisplay(this)}", Target = typeof(ProjectImageMoniker))]

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

        // These internal fields are provided for convenience

        internal static ImageMoniker Abbreviation => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Abbreviation };
        internal static ImageMoniker AboutBox => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.AboutBox };
        internal static ImageMoniker AbsolutePosition => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.AbsolutePosition };
        internal static ImageMoniker Application => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Application };
        internal static ImageMoniker BinaryFile => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.BinaryFile };
        internal static ImageMoniker Blank => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Blank };
        internal static ImageMoniker CodeInformation => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.CodeInformation };
        internal static ImageMoniker CSProjectNode => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.CSProjectNode };
        internal static ImageMoniker CSSharedProject => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.CSSharedProject };
        internal static ImageMoniker FSFileNode => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.FSFileNode };
        internal static ImageMoniker FSProjectNode => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.FSProjectNode };
        internal static ImageMoniker FSScript => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.FSScript };
        internal static ImageMoniker FSSignatureFile => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.FSSignatureFile };
        internal static ImageMoniker GlyphDown => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.GlyphDown };
        internal static ImageMoniker GlyphUp => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.GlyphUp };
        internal static ImageMoniker Library => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Library };
        internal static ImageMoniker Path => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Path };
        internal static ImageMoniker PathIcon => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.PathIcon };
        internal static ImageMoniker PathListBox => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.PathListBox };
        internal static ImageMoniker PathListBoxItem => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.PathListBoxItem };
        internal static ImageMoniker QuestionMark => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.QuestionMark };
        internal static ImageMoniker Reference => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Reference };
        internal static ImageMoniker ReferenceWarning => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.ReferenceWarning };
        // NOTE SharedProject is defined in both manifests
        //      internal static ImageMoniker SharedProject    => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.SharedProject    };
        internal static ImageMoniker Sound => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Sound };
        internal static ImageMoniker StatusError => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.StatusError };
        internal static ImageMoniker TextFile => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.TextFile };
        internal static ImageMoniker Uninstall => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Uninstall };
        internal static ImageMoniker VBProjectNode => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.VBProjectNode };
        internal static ImageMoniker VBSharedProject => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.VBSharedProject };

        #region DebuggerDisplay support for known project system image monikers

        // These methods are called by the debugger, as instructed by the DebuggerDisplayAttributes at the top of the file.

        internal static string ImageMonikerDebugDisplay(ImageMoniker moniker) => DebugDisplay(moniker.Guid, moniker.Id);

        internal static string ProjectImageMonikerDebugDisplay(ProjectImageMoniker moniker) => DebugDisplay(moniker.Guid, moniker.Id);

        private static string DebugDisplay(Guid guid, int id)
        {
            if (guid == s_manifestGuid)
            {
                switch (id)
                {
                    case 0: return nameof(ApplicationPrivate);
                    case 1: return nameof(ApplicationWarning);
                    case 2: return nameof(CodeInformationPrivate);
                    case 3: return nameof(CodeInformationWarning);
                    case 4: return nameof(Component);
                    case 5: return nameof(ComponentPrivate);
                    case 6: return nameof(ComponentWarning);
                    case 7: return nameof(ErrorSmall);
                    case 8: return nameof(LibraryWarning);
                    case 9: return nameof(NuGetGrey);
                    case 10: return nameof(NuGetGreyPrivate);
                    case 11: return nameof(NuGetGreyWarning);
                    case 12: return nameof(ReferenceGroup);
                    case 13: return nameof(ReferenceGroupWarning);
                    case 14: return nameof(ReferencePrivate);
                    case 15: return nameof(Sdk);
                    case 16: return nameof(SdkPrivate);
                    case 17: return nameof(SdkWarning);
                    case 18: return nameof(SharedProject);
                    case 19: return nameof(SharedProjectPrivate);
                    case 20: return nameof(SharedProjectWarning);
                    case 21: return nameof(WarningSmall);
                    case 22: return nameof(Framework);
                    case 23: return nameof(FrameworkPrivate);
                    case 24: return nameof(FrameworkWarning);
                    case 25: return nameof(ProjectImports);
                    case 26: return nameof(TargetFile);
                    case 27: return nameof(TargetFilePrivate);
                }
            }

            if (guid == KnownImageIds.ImageCatalogGuid)
            {
                switch (id)
                {
                    case KnownImageIds.Abbreviation: return nameof(KnownImageIds.Abbreviation);
                    case KnownImageIds.AboutBox: return nameof(KnownImageIds.AboutBox);
                    case KnownImageIds.AbsolutePosition: return nameof(KnownImageIds.AbsolutePosition);
                    case KnownImageIds.Application: return nameof(KnownImageIds.Application);
                    case KnownImageIds.BinaryFile: return nameof(KnownImageIds.BinaryFile);
                    case KnownImageIds.Blank: return nameof(KnownImageIds.Blank);
                    case KnownImageIds.CodeInformation: return nameof(KnownImageIds.CodeInformation);
                    case KnownImageIds.CSProjectNode: return nameof(KnownImageIds.CSProjectNode);
                    case KnownImageIds.CSSharedProject: return nameof(KnownImageIds.CSSharedProject);
                    case KnownImageIds.FSFileNode: return nameof(KnownImageIds.FSFileNode);
                    case KnownImageIds.FSProjectNode: return nameof(KnownImageIds.FSProjectNode);
                    case KnownImageIds.FSScript: return nameof(KnownImageIds.FSScript);
                    case KnownImageIds.FSSignatureFile: return nameof(KnownImageIds.FSSignatureFile);
                    case KnownImageIds.GlyphDown: return nameof(KnownImageIds.GlyphDown);
                    case KnownImageIds.GlyphUp: return nameof(KnownImageIds.GlyphUp);
                    case KnownImageIds.Library: return nameof(KnownImageIds.Library);
                    case KnownImageIds.Path: return nameof(KnownImageIds.Path);
                    case KnownImageIds.PathIcon: return nameof(KnownImageIds.PathIcon);
                    case KnownImageIds.PathListBox: return nameof(KnownImageIds.PathListBox);
                    case KnownImageIds.PathListBoxItem: return nameof(KnownImageIds.PathListBoxItem);
                    case KnownImageIds.QuestionMark: return nameof(KnownImageIds.QuestionMark);
                    case KnownImageIds.Reference: return nameof(KnownImageIds.Reference);
                    case KnownImageIds.ReferenceWarning: return nameof(KnownImageIds.ReferenceWarning);
                    case KnownImageIds.SharedProject: return nameof(KnownImageIds.SharedProject);
                    case KnownImageIds.Sound: return nameof(KnownImageIds.Sound);
                    case KnownImageIds.StatusError: return nameof(KnownImageIds.StatusError);
                    case KnownImageIds.TextFile: return nameof(KnownImageIds.TextFile);
                    case KnownImageIds.Uninstall: return nameof(KnownImageIds.Uninstall);
                    case KnownImageIds.VBProjectNode: return nameof(KnownImageIds.VBProjectNode);
                    case KnownImageIds.VBSharedProject: return nameof(KnownImageIds.VBSharedProject);
                }
            }

            return $"{guid} ({id})";
        }

        #endregion
    }
}
