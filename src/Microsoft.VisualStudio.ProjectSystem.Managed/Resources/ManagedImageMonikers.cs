// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Contains monikers for icons shipped with Managed Project System.
    /// </summary>

    [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
    public static class ManagedImageMonikers
    {
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers")]
        public static ImageMoniker ApplicationPrivate => KnownMonikers.ApplicationPrivate;

        public static ImageMoniker ApplicationWarning => KnownMonikers.ApplicationWarning;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.COM")]
        public static ImageMoniker Component => KnownMonikers.COM;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.COMPrivate")]
        public static ImageMoniker ComponentPrivate => KnownMonikers.COMPrivate;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.COMWarning")]
        public static ImageMoniker ComponentWarning => KnownMonikers.COMWarning;
        public static ImageMoniker LibraryWarning => KnownMonikers.LibraryWarning;
        public static ImageMoniker ReferenceGroup => KnownMonikers.ReferenceGroup;
        public static ImageMoniker ReferenceGroupWarning => KnownMonikers.ReferenceGroupWarning;
        public static ImageMoniker ReferencePrivate => KnownMonikers.ReferencePrivate;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.SDK")]
        public static ImageMoniker Sdk => KnownMonikers.SDK;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.SDKPrivate")]
        public static ImageMoniker SdkPrivate => KnownMonikers.SDKPrivate;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.SDKWarning")]
        public static ImageMoniker SdkWarning => KnownMonikers.SDKWarning;
        public static ImageMoniker SharedProject => KnownMonikers.SharedProject;
        public static ImageMoniker SharedProjectPrivate => KnownMonikers.SharedProjectPrivate;
        public static ImageMoniker SharedProjectWarning => KnownMonikers.SharedProjectWarning; 
        public static ImageMoniker TargetFile => KnownMonikers.TargetFile;
        public static ImageMoniker TargetFilePrivate => KnownMonikers.TargetFilePrivate;
        public static ImageMoniker PropertiesFolderClosed => KnownMonikers.PropertiesFolderClosed;
        public static ImageMoniker PropertiesFolderOpened => KnownMonikers.PropertiesFolderOpen;
        public static ImageMoniker Application => KnownMonikers.Application;
        public static ImageMoniker CodeInformation => KnownMonikers.CodeInformation;
        public static ImageMoniker Library => KnownMonikers.Library;
        public static ImageMoniker Reference => KnownMonikers.Reference;
        public static ImageMoniker ReferenceWarning => KnownMonikers.ReferenceWarning;
        public static ImageMoniker ErrorSmall => throw new NotSupportedException();
        public static ImageMoniker WarningSmall => throw new NotSupportedException();
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.NuGetNoColor")]
        public static ImageMoniker NuGetGrey => KnownMonikers.NuGetNoColor;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.NuGetNoColorPrivate")]
        public static ImageMoniker NuGetGreyPrivate => KnownMonikers.NuGetNoColorPrivate;
        [Obsolete("Please use Microsoft.VisualStudio.Imaging.KnownMonikers.NuGetNoColorWarning")]
        public static ImageMoniker NuGetGreyWarning => KnownMonikers.NuGetNoColorWarning;
        public static ImageMoniker Framework => KnownMonikers.Framework;
        public static ImageMoniker FrameworkPrivate => KnownMonikers.FrameworkPrivate;
        public static ImageMoniker FrameworkWarning => KnownMonikers.FrameworkWarning;
        public static ImageMoniker ProjectImports => KnownMonikers.ProjectImports;
        public static ImageMoniker CodeInformationPrivate => KnownMonikers.CodeInformationPrivate;
        public static ImageMoniker CodeInformationWarning => KnownMonikers.CodeInformationWarning;
    }
}
