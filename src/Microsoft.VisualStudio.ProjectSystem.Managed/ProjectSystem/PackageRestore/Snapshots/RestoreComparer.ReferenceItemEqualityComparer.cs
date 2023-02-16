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

                return x.Properties.SequenceEqual(y.Properties, ReferenceProperties);
            }

            public override int GetHashCode(ReferenceItem? obj)
            {
                if (obj is null)
                    return 0;

                return obj.Name.GetHashCode();
            }
        }
    }
}
