// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class PackageDependencyModel : DependencyModel
    {
        public PackageDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            string name,
            ProjectTreeFlags flags,
            string version,
            bool resolved,
            bool isImplicit,
            bool isTopLevel,
            bool isVisible,
            IImmutableDictionary<string, string> properties,
            IEnumerable<string> dependenciesIDs)
            : base(providerType, path, originalItemSpec, flags, resolved, isImplicit, properties)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            Name = name;
            Version = version;
            Caption = string.IsNullOrEmpty(version) ? name : $"{name} ({version})";
            TopLevel = isTopLevel;
            Visible = isVisible;
            SchemaItemType = PackageReference.PrimaryDataSourceItemType;
            Icon = isImplicit ? ManagedImageMonikers.NuGetGreyPrivate : ManagedImageMonikers.NuGetGrey;
            ExpandedIcon = Icon;
            UnresolvedIcon = ManagedImageMonikers.NuGetGreyWarning;
            UnresolvedExpandedIcon = UnresolvedIcon;

            if (dependenciesIDs != null && dependenciesIDs.Any())
            {
                DependencyIDs = ImmutableList.CreateRange(dependenciesIDs);
            }

            Flags = Flags.Union(DependencyTreeFlags.PackageNodeFlags)
                         .Union(DependencyTreeFlags.SupportsHierarchy);

            if (resolved)
            {
                Priority = Dependency.PackageNodePriority;
                SchemaName = ResolvedPackageReference.SchemaName;
            }
            else
            {
                Priority = Dependency.UnresolvedReferenceNodePriority;
                SchemaName = PackageReference.SchemaName;
            }
        }

        private string _id;
        public override string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = OriginalItemSpec;
                }

                return _id;
            }
        }
    }
}
