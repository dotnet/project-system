// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class SharedProjectDependencyModel : DependencyModel
    {
        public SharedProjectDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(providerType, path, originalItemSpec, flags, resolved, isImplicit, properties)
        {
            if (Resolved)
            {
                SchemaName = ResolvedProjectReference.SchemaName;
            }
            else
            {
                SchemaName = ProjectReference.SchemaName;
            }

            Flags = Flags.Union(DependencyTreeFlags.SharedProjectFlags)
                         .Except(DependencyTreeFlags.SupportsRuleProperties);
            Caption = System.IO.Path.GetFileNameWithoutExtension(Name);
            Priority = Dependency.ProjectNodePriority;
            SchemaItemType = ProjectReference.PrimaryDataSourceItemType;
            Icon = isImplicit ? ManagedImageMonikers.SharedProjectPrivate : KnownMonikers.SharedProject;
            ExpandedIcon = Icon;
            UnresolvedIcon = ManagedImageMonikers.SharedProjectWarning;
            UnresolvedExpandedIcon = UnresolvedIcon;
        }
    }
}
