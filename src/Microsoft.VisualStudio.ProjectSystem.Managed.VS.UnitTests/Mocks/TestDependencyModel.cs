// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class TestDependencyModel : IDependencyModel
    {
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

        public bool Matches(IDependency dependency, ITargetFramework tfm)
        {
            return Dependency.GetID(tfm, ProviderType, Id) == dependency.Id
                   && ProviderType == dependency.ProviderType
                   && Flags == dependency.Flags
                   && (Name == null || Name == dependency.Name)
                   && (Caption == null || Caption == dependency.Caption)
                   && (OriginalItemSpec == null || OriginalItemSpec == dependency.OriginalItemSpec)
                   && (Path == null || OriginalItemSpec == dependency.Path)
                   && (SchemaName == null || SchemaName == dependency.SchemaName)
                   && (SchemaItemType == null || !Flags.Contains(DependencyTreeFlags.GenericDependencyFlags) || SchemaItemType == dependency.SchemaItemType)
                   && (Version == null || Version == dependency.Version)
                   && Resolved == dependency.Resolved
                   && TopLevel == dependency.TopLevel
                   && Implicit == dependency.Implicit
                   && Visible == dependency.Visible
                   && Priority == dependency.Priority
                   && Equals(Icon, dependency.Icon)
                   && Equals(ExpandedIcon, dependency.ExpandedIcon)
                   && Equals(UnresolvedIcon, dependency.UnresolvedIcon)
                   && Equals(UnresolvedExpandedIcon, dependency.UnresolvedExpandedIcon)
                   && Equals(Properties, dependency.Properties)
                   && SetEquals(DependencyIDs, dependency.DependencyIDs);
        }

        private static bool SetEquals(IImmutableList<string> a, IImmutableList<string> b)
        {
            return a.Count == b.Count && new HashSet<string>(a).SetEquals(b);
        }

        private static bool Equals(IImmutableDictionary<string, string> a, IImmutableDictionary<string, string> b)
        {
            // Allow b to have whatever if we didn't specify any properties
            if (a == null || a.Count == 0)
                return true;

            return a.Count == b.Count &&
                   a.All(pair => b.TryGetValue(pair.Key, out var value) && value == pair.Value);
        }

        private static bool Equals(ImageMoniker a, ImageMoniker b) => a.Id == b.Id && a.Guid == b.Guid;
    }
}
