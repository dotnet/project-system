// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class SdkDependencyModel : DependencyModel
    {
        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.Sdk,
            expandedIcon: ManagedImageMonikers.Sdk,
            unresolvedIcon: ManagedImageMonikers.SdkWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.SdkWarning);

        private static readonly DependencyIconSet s_implicitIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.SdkPrivate,
            expandedIcon: ManagedImageMonikers.SdkPrivate,
            unresolvedIcon: ManagedImageMonikers.SdkWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.SdkWarning);

        public SdkDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(providerType, path, originalItemSpec, flags, resolved, isImplicit, properties)
        {
            Flags = Flags.Union(DependencyTreeFlags.SupportsHierarchy);
            SchemaName = Resolved ? ResolvedSdkReference.SchemaName : SdkReference.SchemaName;
            SchemaItemType = SdkReference.PrimaryDataSourceItemType;
            Priority = Dependency.SdkNodePriority;
            IconSet = isImplicit ? s_implicitIconSet : s_iconSet;
            Version = properties != null && properties.ContainsKey(ProjectItemMetadata.Version)
                ? properties[ProjectItemMetadata.Version] 
                : string.Empty;
            string baseCaption = Path.Split(Delimiter.Comma, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            Caption = string.IsNullOrEmpty(Version) ? baseCaption : $"{baseCaption} ({Version})";
        }

        public override string Id => OriginalItemSpec;
    }
}
