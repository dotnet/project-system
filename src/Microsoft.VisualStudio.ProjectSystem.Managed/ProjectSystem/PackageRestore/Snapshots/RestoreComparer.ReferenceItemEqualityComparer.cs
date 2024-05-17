// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    internal static partial class RestoreComparer
    {
        internal class ReferenceItemEqualityComparer : EqualityComparer<ReferenceItem?>
        {
            public override bool Equals(ReferenceItem? x, ReferenceItem? y)
            {
                if (x is null || y is null)
                    return x == y;

                if (!StringComparers.ItemNames.Equals(x.Name, y.Name))
                    return false;

                return Enumerable.SequenceEqual(x.Properties.OrderBy(kvp => kvp.Key), y.Properties.OrderBy(kvp => kvp.Key));
            }

            public override int GetHashCode(ReferenceItem? obj)
            {
                if (obj is null)
                    return 0;

                return StringComparers.ItemNames.GetHashCode(obj.Name);
            }
        }
    }
}
