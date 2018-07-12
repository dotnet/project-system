// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class ComDependencyModel : DependencyModel
    {
        public ComDependencyModel(
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
                Caption = System.IO.Path.GetFileNameWithoutExtension(Name);
                SchemaName = ResolvedCOMReference.SchemaName;
            }
            else
            {
                Caption = Name;
                SchemaName = ComReference.SchemaName;
            }

            SchemaItemType = ComReference.PrimaryDataSourceItemType;
            Priority = Dependency.ComNodePriority;
            Icon = isImplicit ? ManagedImageMonikers.ComponentPrivate : ManagedImageMonikers.Component;
            ExpandedIcon = Icon;
            UnresolvedIcon = ManagedImageMonikers.ComponentWarning;
            UnresolvedExpandedIcon = UnresolvedIcon;
        }
    }
}
