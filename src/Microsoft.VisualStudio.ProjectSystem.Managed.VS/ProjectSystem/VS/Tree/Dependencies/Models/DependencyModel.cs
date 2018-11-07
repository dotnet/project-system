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
            OriginalItemSpec = originalItemSpec ?? path;
            Resolved = resolved;
            Implicit = isImplicit;
            Properties = properties ?? ImmutableStringDictionary<string>.EmptyOrdinal;
            Caption = path;

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
                // Cannot remove implicit dependencies
                Flags = Flags.Except(DependencyTreeFlags.SupportsRemove);
            }

            if (Properties.TryGetValue("Visible", out string visibleMetadata)
                && bool.TryParse(visibleMetadata, out bool visible))
            {
                Visible = visible;
            }
        }

        public abstract string ProviderType { get; }

        public virtual string Name => Path;
        public string Caption { get; protected set; }
        public string OriginalItemSpec { get; }
        public string Path { get; }
        public virtual string SchemaName => null;
        public virtual string SchemaItemType => null;
        public virtual string Version => null;
        public bool Resolved { get; } = false;
        public bool TopLevel { get; protected set; } = true;
        public bool Implicit { get; } = false;
        public bool Visible { get; protected set; } = true;
        public virtual int Priority => 0;
        public ImageMoniker Icon => IconSet.Icon;
        public ImageMoniker ExpandedIcon => IconSet.ExpandedIcon;
        public ImageMoniker UnresolvedIcon => IconSet.UnresolvedIcon;
        public ImageMoniker UnresolvedExpandedIcon => IconSet.UnresolvedExpandedIcon;
        public IImmutableDictionary<string, string> Properties { get; }
        public virtual IImmutableList<string> DependencyIDs => ImmutableList<string>.Empty;
        public ProjectTreeFlags Flags { get; protected set; }

        public abstract DependencyIconSet IconSet { get; }

        public string Id => OriginalItemSpec;

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
