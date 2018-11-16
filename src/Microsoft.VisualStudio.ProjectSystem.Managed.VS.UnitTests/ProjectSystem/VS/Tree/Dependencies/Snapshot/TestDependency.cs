// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [DebuggerDisplay("{" + nameof(Id) +",nq}")]
    internal class TestDependency : IDependency
    {
        public string ProviderType { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public string OriginalItemSpec { get; set; }
        public string Path { get; set; }
        public string FullPath { get; set; }
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
        public DependencyIconSet IconSet { get; set; }

        public IDependency SetProperties(
            string caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            string schemaName = null,
            IImmutableList<string> dependencyIDs = null,
            DependencyIconSet iconSet = null,
            bool? isImplicit = null)
        {
            return new TestDependency
            {
                ProviderType = ProviderType,
                Name = Name,
                Caption = caption ?? Caption,
                OriginalItemSpec = OriginalItemSpec,
                Path = Path,
                FullPath = FullPath,
                SchemaName = schemaName ?? SchemaName,
                SchemaItemType = SchemaItemType,
                Version = Version,
                Resolved = resolved ?? Resolved,
                TopLevel = TopLevel,
                Implicit = isImplicit ?? Implicit,
                Visible = Visible,
                Priority = Priority,
                Icon = iconSet?.Icon ?? Icon,
                ExpandedIcon = iconSet?.ExpandedIcon ?? ExpandedIcon,
                UnresolvedIcon = iconSet?.UnresolvedIcon ?? UnresolvedIcon,
                UnresolvedExpandedIcon = iconSet?.UnresolvedExpandedIcon ?? UnresolvedExpandedIcon,
                Properties = Properties,
                DependencyIDs = dependencyIDs ?? DependencyIDs,
                Flags = flags ?? Flags,
                Id = Id,
                Alias = Alias
            };
        }

        public override int GetHashCode() 
            => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);

        public override bool Equals(object obj) 
            => obj is IDependency other && Equals(other);

        public bool Equals(IDependency other) 
            => other != null && other.Id.Equals(Id, StringComparison.OrdinalIgnoreCase);

        public static bool operator ==(TestDependency left, TestDependency right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(TestDependency left, TestDependency right)
            => !(left == right);

        public static bool operator <(TestDependency left, TestDependency right)
            => left is null ? !(right is null) : left.CompareTo(right) < 0;

        public static bool operator <=(TestDependency left, TestDependency right)
            => left is null || left.CompareTo(right) <= 0;

        public static bool operator >(TestDependency left, TestDependency right)
            => !(left is null) && left.CompareTo(right) > 0;

        public static bool operator >=(TestDependency left, TestDependency right)
            => left is null ? right is null : left.CompareTo(right) >= 0;

        public int CompareTo(IDependency other) 
            => other == null ? 1 : StringComparer.OrdinalIgnoreCase.Compare(Id, other.Id);
    }
}
