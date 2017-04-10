// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class ProjectDependencyModel : DependencyModel
    {
        public ProjectDependencyModel(
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
                SchemaName = ResolvedProjectReference.SchemaName;
            }
            else
            {                
                SchemaName = ProjectReference.SchemaName;
            }

            Caption = System.IO.Path.GetFileNameWithoutExtension(Name);
            SchemaItemType = ProjectReference.PrimaryDataSourceItemType;
            Priority = Dependency.ProjectNodePriority;
            Icon = isImplicit ? ManagedImageMonikers.ApplicationPrivate : KnownMonikers.Application;
            ExpandedIcon = Icon;
            UnresolvedIcon = ManagedImageMonikers.ApplicationWarning;
            UnresolvedExpandedIcon = UnresolvedIcon;
        }
    }
}
