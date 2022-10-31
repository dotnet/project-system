// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    internal static partial class RestoreComparer
    {
        private class ReferencePropertyEqualityComparer : EqualityComparer<IVsReferenceProperty?>
        {
            public override bool Equals(IVsReferenceProperty? x, IVsReferenceProperty? y)
            {
                if (x is null || y is null)
                    return x == y;

                if (!StringComparers.PropertyNames.Equals(x.Name, y.Name))
                    return false;

                return StringComparers.PropertyValues.Equals(x.Value, y.Value);
            }

            public override int GetHashCode(IVsReferenceProperty? obj)
            {
                if (obj is null)
                    return 0;

                return obj.Name.GetHashCode();
            }
        }
    }
}
