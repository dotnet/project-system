// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static partial class RestoreComparer
    {
        private class ProjectPropertyEqualityComparer : EqualityComparer<IVsProjectProperty?>
        {
            public override bool Equals(IVsProjectProperty? x, IVsProjectProperty? y)
            {
                if (x is null || y is null)
                    return x == y;

                if (!StringComparers.PropertyNames.Equals(x.Name, y.Name))
                    return false;

                return StringComparers.PropertyValues.Equals(x.Value, y.Value);
            }

            public override int GetHashCode(IVsProjectProperty? obj)
            {
                if (obj == null)
                    return 0;

                return obj.Name.GetHashCode();
            }
        }
    }
}
