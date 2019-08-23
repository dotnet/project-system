// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static partial class RestoreComparer
    {
        internal class ReferenceItemEqualityComparer : EqualityComparer<IVsReferenceItem?>
        {
            public override bool Equals(IVsReferenceItem? x, IVsReferenceItem? y)
            {
                if (x is null || y is null)
                    return x == y;

                if (!StringComparers.ItemNames.Equals(x.Name, y.Name))
                    return false;

                IEnumerable<IVsReferenceProperty> xProperties = x.Properties.Cast<IVsReferenceProperty>();
                IEnumerable<IVsReferenceProperty> yProperties = y.Properties.Cast<IVsReferenceProperty>();

                return xProperties.SequenceEqual(yProperties, ReferenceProperties);
            }

            public override int GetHashCode(IVsReferenceItem? obj)
            {
                if (obj == null)
                    return 0;

                return obj.Name.GetHashCode();
            }
        }
    }
}
