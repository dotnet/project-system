// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class TargetedDependenciesSnapshotFactory
    {
        public static TargetedDependenciesSnapshot ImplementFromDependencies(IReadOnlyList<IDependency> dependencies)
        {
            var tfm = dependencies.FirstOrDefault(d => d.TargetFramework != null)?.TargetFramework ?? new TargetFramework("tfm");

            var dic = dependencies.ToImmutableDictionary(d => d.Id).WithComparers(StringComparer.OrdinalIgnoreCase);

            return new TargetedDependenciesSnapshot(
                "ProjectPath",
                tfm,
                null,
                dic);
        }

        public static TargetedDependenciesSnapshot ImplementHasUnresolvedDependency(
            bool hasUnresolvedDependency,
            ITargetFramework? targetFramework = null,
            string? id = null)
        {
            id ??= "Id";

            var dic = new Dictionary<string, IDependency>
            {
                { id, new TestDependency { Id = id, Resolved = !hasUnresolvedDependency } }
            };

            return new TargetedDependenciesSnapshot(
                "ProjectPath",
                targetFramework ?? new TargetFramework("tfm"),
                null,
                dic.ToImmutableDictionary().WithComparers(StringComparer.OrdinalIgnoreCase));
        }
    }
}
