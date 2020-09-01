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

        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker ApplicationPrivate => KnownMonikers.ApplicationPrivate;

        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker ApplicationWarning => KnownMonikers.ApplicationWarning;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.COM")]
        public static ImageMoniker Component => KnownMonikers.COM;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.COMPrivate")]
        public static ImageMoniker ComponentPrivate => KnownMonikers.COMPrivate;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.COMWarning")]
        public static ImageMoniker ComponentWarning => KnownMonikers.COMWarning;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker LibraryWarning => KnownMonikers.LibraryWarning;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker ReferenceGroup => KnownMonikers.ReferenceGroup;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker ReferenceGroupWarning => KnownMonikers.ReferenceGroupWarning;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker ReferencePrivate => KnownMonikers.ReferencePrivate;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.SDK")]
        public static ImageMoniker Sdk => KnownMonikers.SDK;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.SDKPrivate")]
        public static ImageMoniker SdkPrivate => KnownMonikers.SDKPrivate;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.SDKWarning")]
        public static ImageMoniker SdkWarning => KnownMonikers.SDKWarning;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker SharedProject => KnownMonikers.SharedProject;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker SharedProjectPrivate => KnownMonikers.SharedProjectPrivate;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker SharedProjectWarning => KnownMonikers.SharedProjectWarning; 
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker TargetFile => KnownMonikers.TargetFile;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker TargetFilePrivate => KnownMonikers.TargetFilePrivate;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker PropertiesFolderClosed => KnownMonikers.PropertiesFolderClosed;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker PropertiesFolderOpened => KnownMonikers.PropertiesFolderOpen;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker Application => KnownMonikers.Application;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker CodeInformation => KnownMonikers.CodeInformation;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker Library => KnownMonikers.Library;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker Reference => KnownMonikers.Reference;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker ReferenceWarning => KnownMonikers.ReferenceWarning;

        // Everything below this, still needs to be moved to the ImageCatalog
        public static ImageMoniker ErrorSmall => new ImageMoniker { Guid = s_manifestGuid, Id = 7 };
        public static ImageMoniker WarningSmall => new ImageMoniker { Guid = s_manifestGuid, Id = 21 };
        public static ImageMoniker NuGetGrey => new ImageMoniker { Guid = s_manifestGuid, Id = 9 };
        public static ImageMoniker NuGetGreyPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 10 };
        public static ImageMoniker NuGetGreyWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 11 };
        public static ImageMoniker Framework => new ImageMoniker { Guid = s_manifestGuid, Id = 22 };
        public static ImageMoniker FrameworkPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 23 };
        public static ImageMoniker FrameworkWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 24 };
        public static ImageMoniker ProjectImports => new ImageMoniker { Guid = s_manifestGuid, Id = 25 };
        public static ImageMoniker CodeInformationPrivate => new ImageMoniker { Guid = s_manifestGuid, Id = 2 };
        public static ImageMoniker CodeInformationWarning => new ImageMoniker { Guid = s_manifestGuid, Id = 3 };

    }
}
