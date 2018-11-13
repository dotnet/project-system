// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public interface IDependenciesChanges
    {
        bool AnyChanges { get; }

        ImmutableArray<IDependencyModel> AddedNodes { get; }

        ImmutableArray<RemovedDependencyIdentity> RemovedNodes { get; }
    }

    [DebuggerDisplay("({" + nameof(ProviderType) + ("}, {" + nameof(DependencyId) + "})"))]
    public readonly struct RemovedDependencyIdentity : IEquatable<RemovedDependencyIdentity>
    {
        public string ProviderType { get; }
        public string DependencyId { get; }

        public RemovedDependencyIdentity(string providerType, string dependencyId) : this()
        {
            Requires.NotNull(providerType, nameof(providerType));
            Requires.NotNull(dependencyId, nameof(dependencyId));

            ProviderType = providerType;
            DependencyId = dependencyId;
        }

        public void Deconstruct(out string providerType, out string dependencyId)
        {
            providerType = ProviderType;
            dependencyId = DependencyId;
        }

        public bool Equals(RemovedDependencyIdentity other)
        {
            return string.Equals(ProviderType, other.ProviderType, StringComparisons.DependencyProviderTypes) && 
                   string.Equals(DependencyId, other.DependencyId, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) => obj is RemovedDependencyIdentity other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(ProviderType) * 397) ^
                        StringComparer.OrdinalIgnoreCase.GetHashCode(DependencyId);
            }
        }

        public static bool operator ==(RemovedDependencyIdentity left, RemovedDependencyIdentity right) => left.Equals(right);
        public static bool operator !=(RemovedDependencyIdentity left, RemovedDependencyIdentity right) => !left.Equals(right);
    }
}
