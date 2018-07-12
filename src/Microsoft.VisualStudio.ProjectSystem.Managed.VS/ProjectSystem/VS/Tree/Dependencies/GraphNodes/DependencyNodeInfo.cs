// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    internal struct DependencyNodeInfo : IEquatable<DependencyNodeInfo>
    {
        public string Id;
        public string Caption;
        public bool Resolved;

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
        }

        public override bool Equals(object obj)
        {
            if (obj is DependencyNodeInfo other)
            {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(DependencyNodeInfo other)
        {
            return other.GetHashCode() == GetHashCode();
        }

        public static DependencyNodeInfo FromDependency(IDependency dependency)
        {
            Requires.NotNull(dependency, nameof(dependency));

            return new DependencyNodeInfo
            {
                Id = dependency.Id,
                Caption = dependency.Caption,
                Resolved = dependency.Resolved
            };
        }
    }
}
