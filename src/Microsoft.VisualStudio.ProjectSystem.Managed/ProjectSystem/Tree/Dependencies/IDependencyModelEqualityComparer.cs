// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
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
