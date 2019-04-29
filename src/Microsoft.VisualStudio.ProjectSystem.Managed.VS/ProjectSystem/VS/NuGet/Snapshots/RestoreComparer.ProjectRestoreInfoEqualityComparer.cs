// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal static partial class RestoreComparer
    {
        private class ProjectRestoreInfoEqualityComparer : EqualityComparer<IVsProjectRestoreInfo>
        {
            public override bool Equals(IVsProjectRestoreInfo x, IVsProjectRestoreInfo y)
            {
                if (x is null || y is null)
                    return x == y;

                if (!StringComparers.PropertyValues.Equals(x.BaseIntermediatePath, y.BaseIntermediatePath))
                    return false;

                if (!StringComparers.PropertyValues.Equals(x.OriginalTargetFrameworks, y.OriginalTargetFrameworks))
                    return false;

                IEnumerable<IVsTargetFrameworkInfo> xTargetFrameworks = x.TargetFrameworks.Cast<IVsTargetFrameworkInfo>();
                IEnumerable<IVsTargetFrameworkInfo> yTargetFrameworks = y.TargetFrameworks.Cast<IVsTargetFrameworkInfo>();

                if (!xTargetFrameworks.SequenceEqual(yTargetFrameworks, TargetFrameworks))
                    return false;

                IEnumerable<IVsReferenceItem> xToolReferences = x.ToolReferences.Cast<IVsReferenceItem>();
                IEnumerable<IVsReferenceItem> yToolReferences = y.ToolReferences.Cast<IVsReferenceItem>();

                return xToolReferences.SequenceEqual(yToolReferences, ReferenceItems);
            }

            public override int GetHashCode(IVsProjectRestoreInfo obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
