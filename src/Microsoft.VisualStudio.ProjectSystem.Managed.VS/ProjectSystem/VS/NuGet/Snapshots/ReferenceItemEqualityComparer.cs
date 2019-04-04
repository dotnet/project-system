// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class ReferenceItemEqualityComparer : EqualityComparer<IVsReferenceItem>
    {
        public readonly static ReferenceItemEqualityComparer Instance = new ReferenceItemEqualityComparer();

        public override bool Equals(IVsReferenceItem x, IVsReferenceItem y)
        {
            if (x is null || y is null)
                return x == y;

            if (!StringComparers.ItemNames.Equals(x.Name, y.Name))
                return false;

            if (x.Properties.Count != y.Properties.Count)
                return false;

            if (x.Properties.Count == 0)
                return true;

            foreach (IVsReferenceProperty xProperty in x.Properties)
            {
                IVsReferenceProperty yProperty = y.Properties.Item(xProperty.Name);
                if (yProperty == null)
                    return false;

                if (!StringComparers.PropertyNames.Equals(xProperty.Value, yProperty.Value))
                    return false;
            }

            return true;
        }

        public override int GetHashCode(IVsReferenceItem obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
