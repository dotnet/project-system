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

        public override DependencyIconSet IconSet => Implicit ? s_implicitIconSet : s_iconSet;

        public override string Id => OriginalItemSpec;

        public override int Priority => Dependency.SdkNodePriority;

        public override string ProviderType => SdkRuleHandler.ProviderTypeString;

        public override string SchemaItemType => SdkReference.PrimaryDataSourceItemType;

        public override string SchemaName => Resolved ? ResolvedSdkReference.SchemaName : SdkReference.SchemaName;

        public override string Version { get; }

        public SdkDependencyModel(
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(path, originalItemSpec, flags, resolved, isImplicit, properties)
        {
            Flags = Flags.Union(DependencyTreeFlags.SupportsHierarchy);
            Version = properties != null && properties.ContainsKey(ProjectItemMetadata.Version)
                ? properties[ProjectItemMetadata.Version] 
                : string.Empty;
            string baseCaption = Path.Split(Delimiter.Comma, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            Caption = string.IsNullOrEmpty(Version) ? baseCaption : $"{baseCaption} ({Version})";
        }
    }
}
