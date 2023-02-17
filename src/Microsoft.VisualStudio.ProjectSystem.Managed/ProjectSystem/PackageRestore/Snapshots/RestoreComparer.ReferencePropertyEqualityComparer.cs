// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    internal static partial class RestoreComparer
    {
        private class ReferencePropertyEqualityComparer : EqualityComparer<ReferenceProperty?>
        {
            public override bool Equals(ReferenceProperty? x, ReferenceProperty? y)
            {
                if (x is null || y is null)
                    return x == y;

                if (!StringComparers.PropertyNames.Equals(x.Name, y.Name))
                    return false;

                return StringComparers.PropertyValues.Equals(x.Value, y.Value);
            }

            public override int GetHashCode(ReferenceProperty? obj)
            {
                if (obj is null)
                    return 0;

                return obj.Name.GetHashCode();
            }
        }
    }
}
