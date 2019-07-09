// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static partial class RestoreComparer
    {
        private class ProjectRestoreInfoEqualityComparer : EqualityComparer<IVsProjectRestoreInfo2?>
        {
            public override bool Equals(IVsProjectRestoreInfo2? x, IVsProjectRestoreInfo2? y)
            {
                if (x is null || y is null)
                    return x == y;

                if (!StringComparers.PropertyValues.Equals(x.BaseIntermediatePath, y.BaseIntermediatePath))
                    return false;

                if (!StringComparers.PropertyValues.Equals(x.OriginalTargetFrameworks, y.OriginalTargetFrameworks))
                    return false;

                IEnumerable<IVsTargetFrameworkInfo2> xTargetFrameworks = x.TargetFrameworks.Cast<IVsTargetFrameworkInfo2>();
                IEnumerable<IVsTargetFrameworkInfo2> yTargetFrameworks = y.TargetFrameworks.Cast<IVsTargetFrameworkInfo2>();

                if (!xTargetFrameworks.SequenceEqual(yTargetFrameworks, TargetFrameworks))
                    return false;

                IEnumerable<IVsReferenceItem> xToolReferences = x.ToolReferences.Cast<IVsReferenceItem>();
                IEnumerable<IVsReferenceItem> yToolReferences = y.ToolReferences.Cast<IVsReferenceItem>();

                return xToolReferences.SequenceEqual(yToolReferences, ReferenceItems);
            }

            public override int GetHashCode(IVsProjectRestoreInfo2? obj)
            {
                if (obj == null)
                    return 0;

                return obj.GetHashCode();
            }
        }
    }
}
