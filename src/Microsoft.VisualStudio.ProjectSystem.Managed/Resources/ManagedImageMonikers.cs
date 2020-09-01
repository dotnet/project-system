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
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.COM")]
        public static ImageMoniker Component => new ImageMoniker { Guid = s_manifestGuid, Id = 4 };
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.COMPrivate")]
        public static ImageMoniker ComponentPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 5 };
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.COMWarning")]
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
        public static ImageMoniker Application => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Application };
        public static ImageMoniker CodeInformation => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.CodeInformation };
        public static ImageMoniker Library => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Library };
        public static ImageMoniker Reference => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.Reference };
        public static ImageMoniker ReferenceWarning => new ImageMoniker { Guid = KnownImageIds.ImageCatalogGuid, Id = KnownImageIds.ReferenceWarning };
    }
}
