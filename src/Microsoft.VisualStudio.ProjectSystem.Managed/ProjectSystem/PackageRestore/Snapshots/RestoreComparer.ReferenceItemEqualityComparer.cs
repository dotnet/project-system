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

                if (x.Properties.Count != y.Properties.Count)
                    return false;

                foreach (KeyValuePair<string, string> property in x.Properties)
                {
                    if (!y.Properties.TryGetValue(property.Key, out string yValue))
                    {
                        return false;
                    }

                    if (!StringComparer.Ordinal.Equals(Equals(property.Key, yValue)))
                    {
                        return false;
                    }
                }

                return true;
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
