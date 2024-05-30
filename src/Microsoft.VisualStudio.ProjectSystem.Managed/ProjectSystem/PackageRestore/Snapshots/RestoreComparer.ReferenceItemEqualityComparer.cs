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

                if (!PropertiesAreEqual(x.Properties, y.Properties))
                    return false;

                return true;

                static bool PropertiesAreEqual(IImmutableDictionary<string, string> x, IImmutableDictionary<string, string> y)
                {
                    if (x.Count != y.Count)
                        return false;

                    foreach ((string xKey, string xValue) in x)
                    {
                        if (!y.TryGetValue(xKey, out string yValue))
                        {
                            return false;
                        }

                        if (!StringComparers.PropertyValues.Equals(xValue, yValue))
                        {
                            return false;
                        }
                    }
                    return true;
                }
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
