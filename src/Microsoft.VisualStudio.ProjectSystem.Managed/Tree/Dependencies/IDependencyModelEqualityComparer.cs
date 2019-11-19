// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal sealed class IDependencyModelEqualityComparer : IEqualityComparer<IDependencyModel>
    {
        public static IDependencyModelEqualityComparer Instance { get; } = new IDependencyModelEqualityComparer();

        public bool Equals(IDependencyModel? x, IDependencyModel? y)
        {
            if (x is null && y is null)
                return true;

            if (x is null || y is null)
                return false;

            return string.Equals(x.Id, y.Id, StringComparisons.DependencyTreeIds) &&
                   string.Equals(x.ProviderType, y.ProviderType, StringComparisons.DependencyProviderTypes);
        }

        public int GetHashCode(IDependencyModel obj)
        {
            Requires.NotNull(obj, nameof(obj));

            return unchecked(StringComparers.DependencyTreeIds.GetHashCode(obj.Id) * 397 ^
                             StringComparers.DependencyProviderTypes.GetHashCode(obj.ProviderType));
        }
    }
}
