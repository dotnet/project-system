// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal class TestDependency : IDependency
    {
        public TestDependency()
        {
        }

        public string ProviderType { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public string OriginalItemSpec { get; set; }
        public string Path { get; set; }
        public string SchemaName { get; set; }
        public string SchemaItemType { get; set; }
        public string Version { get; set; }
        public bool Resolved { get; set; } = false;
        public bool TopLevel { get; set; } = true;
        public bool Implicit { get; set; } = false;
        public bool Visible { get; set; } = true;
        public int Priority { get; set; } = 0;
        public ImageMoniker Icon { get; set; }
        public ImageMoniker ExpandedIcon { get; set; }
        public ImageMoniker UnresolvedIcon { get; set; }
        public ImageMoniker UnresolvedExpandedIcon { get; set; }
        public IImmutableDictionary<string, string> Properties { get; set; }
        public IImmutableList<string> DependencyIDs { get; set; } = ImmutableList<string>.Empty;
        public ProjectTreeFlags Flags { get; set; } = ProjectTreeFlags.Empty;
        public string Id { get; set; }
        public string Alias { get; set; }
        public ITargetFramework TargetFramework { get; set; }

        public IDependency SetProperties(
            string caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            IImmutableList<string> dependencyIDs = null)
        {
            return this;
        }

        public override int GetHashCode()
        {
            return unchecked(Id.ToLowerInvariant().GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (obj is IDependency other)
            {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(IDependency other)
        {
            if (other != null && other.Id.Equals(Id, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public int CompareTo(IDependency other)
        {
            if (other == null)
            {
                return 1;
            }

            return StringComparer.OrdinalIgnoreCase.Compare(Id, other.Id);
        }
    }
}
