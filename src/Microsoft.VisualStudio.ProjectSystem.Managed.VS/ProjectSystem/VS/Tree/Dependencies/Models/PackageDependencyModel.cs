// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class PackageDependencyModel : DependencyModel
    {
        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.NuGetGrey,
            expandedIcon: ManagedImageMonikers.NuGetGrey,
            unresolvedIcon: ManagedImageMonikers.NuGetGreyWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.NuGetGreyWarning);

        private static readonly DependencyIconSet s_implicitIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.NuGetGreyPrivate,
            expandedIcon: ManagedImageMonikers.NuGetGreyPrivate,
            unresolvedIcon: ManagedImageMonikers.NuGetGreyWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.NuGetGreyWarning);

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
            IconSet = isImplicit ? s_implicitIconSet : s_iconSet;

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
