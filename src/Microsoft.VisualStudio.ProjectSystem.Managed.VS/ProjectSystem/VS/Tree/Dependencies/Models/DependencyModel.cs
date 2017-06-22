// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class DependencyModel : IDependencyModel
    {
        public DependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            Requires.NotNullOrEmpty(providerType, nameof(providerType));
            Requires.NotNullOrEmpty(path, nameof(path));

            ProviderType = providerType;
            Path = path;
            Name = Path;
            OriginalItemSpec = originalItemSpec ?? Path;
            Resolved = resolved;
            Implicit = isImplicit;
            Properties = properties ?? ImmutableDictionary<string, string>.Empty;
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
        }

        public string ProviderType { get; protected set; }
        public string Name { get; protected set; }
        public string Caption { get; protected set; }
        public string OriginalItemSpec { get; protected set; }
        public string Path { get; protected set; }
        public string SchemaName { get; protected set; }
        public string SchemaItemType { get; protected set; }
        public string Version { get; protected set; }
        public bool Resolved { get; protected set; } = false;
        public bool TopLevel { get; protected set; } = true;
        public bool Implicit { get; protected set; } = false;
        public bool Visible { get; protected set; } = true;
        public int Priority { get; protected set; } = 0;
        public ImageMoniker Icon { get; protected set; }
        public ImageMoniker ExpandedIcon { get; protected set; }
        public ImageMoniker UnresolvedIcon { get; protected set; }
        public ImageMoniker UnresolvedExpandedIcon { get; protected set; }
        public IImmutableDictionary<string, string> Properties { get; protected set; }
        public IImmutableList<string> DependencyIDs { get; protected set; } = ImmutableList<string>.Empty;
        public ProjectTreeFlags Flags { get; protected set; } = ProjectTreeFlags.Empty;

        private string _id;
        public virtual string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = $"{OriginalItemSpec}\\{Version}".TrimEnd('\\');
                }

                return _id;
            }
        }

        public override int GetHashCode()
        {
            return unchecked(StringComparer.OrdinalIgnoreCase.GetHashCode(Id) 
                             + StringComparer.OrdinalIgnoreCase.GetHashCode(ProviderType));
        }

        public override bool Equals(object obj)
        {
            if (obj is IDependencyModel other)
            {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(IDependencyModel other)
        {
            if (other != null 
                && other.Id.Equals(Id, StringComparison.OrdinalIgnoreCase)
                && other.ProviderType.Equals(ProviderType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
