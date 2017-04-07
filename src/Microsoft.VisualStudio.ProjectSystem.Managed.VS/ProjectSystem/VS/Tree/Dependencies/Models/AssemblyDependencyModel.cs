// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class AssemblyDependencyModel : DependencyModel
    {
        public AssemblyDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(providerType, path, originalItemSpec, flags, resolved, isImplicit, properties)
        {
            string fusionName = null;
            if (Properties != null)
            {
                Properties.TryGetValue(ResolvedAssemblyReference.FusionNameProperty, out fusionName);
            }

            if (Resolved)
            {
                if (!string.IsNullOrEmpty(fusionName))
                {
                    var assemblyName = new AssemblyName(fusionName);
                    Caption = assemblyName.Name;
                }
                else
                {
                    Caption = Name;
                }

                SchemaName = ResolvedAssemblyReference.SchemaName;
            }
            else
            {
                Caption = Name;
                SchemaName = AssemblyReference.SchemaName;
            }

            SchemaItemType = AssemblyReference.PrimaryDataSourceItemType;
            Priority = Dependency.FrameworkAssemblyNodePriority;
            Icon = isImplicit ? ManagedImageMonikers.ReferencePrivate : KnownMonikers.Reference;
            ExpandedIcon = Icon;
            UnresolvedIcon = KnownMonikers.ReferenceWarning;
            UnresolvedExpandedIcon = UnresolvedIcon;
        }
    }
}
