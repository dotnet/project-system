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
        private static readonly Guid ManifestGuid = new Guid("{259567C1-AA6B-46BF-811C-C145DD9F3B48}");

        private const int ApplicationPrivateId = 0;
        private const int ApplicationWarningId = 1;
        private const int CodeInformationPrivateId = 2;
        private const int CodeInformationWarningId = 3;
        private const int ComponentId = 4;
        private const int ComponentPrivateId = 5;
        private const int ComponentWarningId = 6;
        private const int ErrorSmallId = 7;
        private const int LibraryWarningId = 8;
        private const int NuGetGreyId = 9;
        private const int NuGetGreyPrivateId = 10;
        private const int NuGetGreyWarningId = 11;
        private const int ReferenceGroupId = 12;
        private const int ReferenceGroupWarningId = 13;
        private const int ReferencePrivateId = 14;
        private const int SdkId = 15;
        private const int SdkPrivateId = 16;
        private const int SdkWarningId = 17;
        private const int SharedProjectId = 18;
        private const int SharedProjectPrivateId = 19;
        private const int SharedProjectWarningId = 20;
        private const int WarningSmallId = 21;

        public static ImageMoniker ApplicationPrivate
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ApplicationPrivateId };
            }
        }

        public static ImageMoniker ApplicationWarning
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ApplicationWarningId };
            }
        }

        public static ImageMoniker CodeInformationPrivate
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = CodeInformationPrivateId };
            }
        }

        public static ImageMoniker CodeInformationWarning
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = CodeInformationWarningId };
            }
        }

        public static ImageMoniker Component
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ComponentId };
            }
        }

        public static ImageMoniker ComponentPrivate
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ComponentPrivateId };
            }
        }

        public static ImageMoniker ComponentWarning
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ComponentWarningId };
            }
        }

        public static ImageMoniker ErrorSmall
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ErrorSmallId };
            }
        }

        public static ImageMoniker LibraryWarning
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = LibraryWarningId };
            }
        }

        public static ImageMoniker NuGetGrey
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = NuGetGreyId };
            }
        }

        public static ImageMoniker NuGetGreyPrivate
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = NuGetGreyPrivateId };
            }
        }

        public static ImageMoniker NuGetGreyWarning
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = NuGetGreyWarningId };
            }
        }

        public static ImageMoniker ReferenceGroup
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ReferenceGroupId };
            }
        }

        public static ImageMoniker ReferencePrivate
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ReferencePrivateId };
            }
        }

        public static ImageMoniker ReferenceGroupWarning
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ReferenceGroupWarningId };
            }
        }

        public static ImageMoniker Sdk
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = SdkId };
            }
        }

        public static ImageMoniker SdkPrivate
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = SdkPrivateId };
            }
        }

        public static ImageMoniker SdkWarning
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = SdkWarningId };
            }
        }

        public static ImageMoniker SharedProject
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = SharedProjectId };
            }
        }

        public static ImageMoniker SharedProjectPrivate
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = SharedProjectPrivateId };
            }
        }

        public static ImageMoniker SharedProjectWarning
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = SharedProjectWarningId };
            }
        }

        public static ImageMoniker WarningSmall
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = WarningSmallId };
            }
        }
    }
}
