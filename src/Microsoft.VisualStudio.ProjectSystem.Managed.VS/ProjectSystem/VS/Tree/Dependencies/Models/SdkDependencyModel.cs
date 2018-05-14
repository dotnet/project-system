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

            if (Resolved)
            {
                SchemaName = ResolvedSdkReference.SchemaName;
            }
            else
            {
                SchemaName = SdkReference.SchemaName;
            }

            SchemaItemType = SdkReference.PrimaryDataSourceItemType;
            Priority = Dependency.SdkNodePriority;
            Icon = isImplicit ? ManagedImageMonikers.SdkPrivate : ManagedImageMonikers.Sdk;
            ExpandedIcon = Icon;
            UnresolvedIcon = ManagedImageMonikers.SdkWarning;
            UnresolvedExpandedIcon = UnresolvedIcon;
            Version = properties != null && properties.ContainsKey(ProjectItemMetadata.Version)
                        ? properties[ProjectItemMetadata.Version] : string.Empty;
            string baseCaption = Path.Split(Delimiter.CommaDelimiter, StringSplitOptions.RemoveEmptyEntries)
                                .FirstOrDefault();
            Caption = string.IsNullOrEmpty(Version) ? baseCaption : $"{baseCaption} ({Version})";
        }

        public override string Id => OriginalItemSpec;
    }
}
