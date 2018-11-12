// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    public readonly struct RemovedDependencyIdentity
    {
        public string ProviderType { get; }
        public string DependencyId { get; }

        public RemovedDependencyIdentity(string providerType, string dependencyId) : this()
        {
            ProviderType = providerType;
            DependencyId = dependencyId;
        }

        public void Deconstruct(out string providerType, out string dependencyId)
        {
            providerType = ProviderType;
            dependencyId = DependencyId;
        }
    }
}
