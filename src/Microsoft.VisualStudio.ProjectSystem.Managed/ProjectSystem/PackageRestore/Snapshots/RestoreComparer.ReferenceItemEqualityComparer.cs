// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
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
                if (obj is null)
                    return 0;

                return obj.Name.GetHashCode();
            }
        }
    }
}
