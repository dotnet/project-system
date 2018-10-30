// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal abstract class DependencyModel : IDependencyModel
    {
        protected DependencyModel(
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            Path = path;
            Name = path;
            OriginalItemSpec = originalItemSpec ?? path;
            Resolved = resolved;
            Implicit = isImplicit;
            Properties = properties ?? ImmutableStringDictionary<string>.EmptyOrdinal;
            Caption = Name;

            if (resolved)
            {
                Flags = flags.Union(DependencyTreeFlags.GenericResolvedDependencyFlags);
            }
            else
            {
                Flags = flags.Union(DependencyTreeFlags.GenericUnresolvedDependencyFlags);
            }

            if (isImplicit)
            {
                Flags = Flags.Except(DependencyTreeFlags.SupportsRemove);
            }

            if (Properties.TryGetValue("Visible", out string visibleMetadata)
                && bool.TryParse(visibleMetadata, out bool visible))
            {
                Visible = visible;
            }
        }

        public abstract string ProviderType { get; }

        public string Name { get; protected set; }
        public string Caption { get; protected set; }
        public string OriginalItemSpec { get; }
        public string Path { get; protected set; }
        public string SchemaName { get; protected set; }
        public string SchemaItemType { get; protected set; }
        public string Version { get; protected set; }
        public bool Resolved { get; protected set; } = false;
        public bool TopLevel { get; protected set; } = true;
        public bool Implicit { get; protected set; } = false;
        public bool Visible { get; protected set; } = true;
        public int Priority { get; protected set; } = 0;
        public ImageMoniker Icon => IconSet.Icon;
        public ImageMoniker ExpandedIcon => IconSet.ExpandedIcon;
        public ImageMoniker UnresolvedIcon => IconSet.UnresolvedIcon;
        public ImageMoniker UnresolvedExpandedIcon => IconSet.UnresolvedExpandedIcon;
        public IImmutableDictionary<string, string> Properties { get; protected set; }
        public IImmutableList<string> DependencyIDs { get; protected set; } = ImmutableList<string>.Empty;
        public ProjectTreeFlags Flags { get; protected set; }

        public DependencyIconSet IconSet { get; protected set; }

        private string _id;
        public virtual string Id
        {
            get
            {
                if (_id == null)
                {
                    if (string.IsNullOrEmpty(Version))
                        _id = OriginalItemSpec;
                    else
                        _id = $"{OriginalItemSpec}\\{Version}".TrimEnd(Delimiter.BackSlash);
                }

                return _id;
            }
        }

        public override int GetHashCode()
        {
            return unchecked(
                StringComparer.OrdinalIgnoreCase.GetHashCode(Id) +
                StringComparers.DependencyProviderTypes.GetHashCode(ProviderType));
        }

        public override bool Equals(object obj)
        {
            return obj is IDependencyModel other && Equals(other);
        }

        public bool Equals(IDependencyModel other)
        {
            return other != null
                && other.Id.Equals(Id, StringComparison.OrdinalIgnoreCase)
                && StringComparers.DependencyProviderTypes.Equals(other.ProviderType, ProviderType);
        }

        public override string ToString() => Id;
    }
}
