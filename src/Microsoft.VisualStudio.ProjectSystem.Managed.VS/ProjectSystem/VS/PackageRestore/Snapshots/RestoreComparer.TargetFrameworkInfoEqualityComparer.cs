// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static partial class RestoreComparer
    {
        private class TargetFrameworkInfoEqualityComparer : EqualityComparer<IVsTargetFrameworkInfo2?>
        {
            public override bool Equals(IVsTargetFrameworkInfo2? x, IVsTargetFrameworkInfo2? y)
            {
                if (x is null || y is null)
                    return x == y;

                if (!StringComparers.ItemNames.Equals(x.TargetFrameworkMoniker, y.TargetFrameworkMoniker))
                    return false;

                IEnumerable<IVsProjectProperty> xProperties = x.Properties.Cast<IVsProjectProperty>();
                IEnumerable<IVsProjectProperty> yProperties = y.Properties.Cast<IVsProjectProperty>();

                if (!xProperties.SequenceEqual(yProperties, ProjectProperties))
                    return false;

                IEnumerable<IVsReferenceItem> xFrameworkReferences = x.FrameworkReferences.Cast<IVsReferenceItem>();
                IEnumerable<IVsReferenceItem> yFrameworkReferences = y.FrameworkReferences.Cast<IVsReferenceItem>();

                if (!xFrameworkReferences.SequenceEqual(yFrameworkReferences, ReferenceItems))
                    return false;

                IEnumerable<IVsReferenceItem> xPackageDownloads = x.PackageDownloads.Cast<IVsReferenceItem>();
                IEnumerable<IVsReferenceItem> yPackageDownloads = y.PackageDownloads.Cast<IVsReferenceItem>();

                if (!xPackageDownloads.SequenceEqual(yPackageDownloads, ReferenceItems))
                    return false;

                IEnumerable<IVsReferenceItem> xProjectReferences = x.ProjectReferences.Cast<IVsReferenceItem>();
                IEnumerable<IVsReferenceItem> yProjectReferences = y.ProjectReferences.Cast<IVsReferenceItem>();

                if (!xProjectReferences.SequenceEqual(yProjectReferences, ReferenceItems))
                    return false;

                IEnumerable<IVsReferenceItem> xPackageReferences = x.PackageReferences.Cast<IVsReferenceItem>();
                IEnumerable<IVsReferenceItem> yPackageReferences = y.PackageReferences.Cast<IVsReferenceItem>();

                return xPackageReferences.SequenceEqual(yPackageReferences, ReferenceItems);
            }

            public override int GetHashCode(IVsTargetFrameworkInfo2? obj)
            {
                if (obj == null)
                    return 0;

                return obj.TargetFrameworkMoniker.GetHashCode();
            }
        }
    }
}
