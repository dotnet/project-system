// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Contains monikers for icons shipped with Managed Project System. Icons are located in
    /// ManagedImages.imagemanifest file that is also installed to VS extension path.
    /// </summary>
    public static class ManagedImageMonikers
    {
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
    }
}
