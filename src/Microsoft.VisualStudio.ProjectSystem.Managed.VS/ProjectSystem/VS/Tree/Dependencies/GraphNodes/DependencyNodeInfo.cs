// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    internal readonly struct DependencyNodeInfo : IEquatable<DependencyNodeInfo>
    {
        public string Id { get; }
        public string Caption { get; }
        public bool Resolved { get; }

        public DependencyNodeInfo(string id, string caption, bool resolved)
        {
            Id = id;
            Caption = caption;
            Resolved = resolved;
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);

        public override bool Equals(object obj) => obj is DependencyNodeInfo other && Equals(other);

        public bool Equals(DependencyNodeInfo other) => StringComparer.OrdinalIgnoreCase.Equals(Id, other.Id);

        public static DependencyNodeInfo FromDependency(IDependency dependency)
        {
            Requires.NotNull(dependency, nameof(dependency));

            return new DependencyNodeInfo(dependency.Id, dependency.Caption, dependency.Resolved);
        }
    }
}
